using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Input;

namespace Snowberry.Editor;

using Element = BinaryPacker.Element;

public class Room {
    public string Name;

    public Rectangle Bounds;

    public Map Map { get; private set; }

    public int X => Bounds.X;
    public int Y => Bounds.Y;
    public int Width => Bounds.Width;
    public int Height => Bounds.Height;
    public Vector2 Position => new(X, Y);
    public Vector2 Size => new(Width, Height);

    public Rectangle ScissorRect { get; private set; }

    // Music data
    public string Music = "";
    public string AltMusic = "";
    public string Ambience = "";
    public bool[] MusicLayers = new bool[4];

    public int MusicProgress;
    public int AmbienceProgress;

    // Camera offset data
    public Vector2 CameraOffset;

    // Misc data
    public bool Dark;
    public bool Underwater;
    public bool Space;
    public bool DisableDownTransition;
    public WindController.Patterns WindPattern = WindController.Patterns.None;

    // Tiles
    private VirtualMap<char> fgTileMap;
    private VirtualMap<char> bgTileMap;
    private VirtualMap<MTexture> fgTiles, bgTiles;

    public readonly List<Decal> FgDecals = new();
    public readonly List<Decal> BgDecals = new();

    public readonly List<Entity> Entities = new();
    public readonly List<Entity> Triggers = new();
    public readonly List<Entity> AllEntities = new();

    public readonly Dictionary<Type, List<Entity>> TrackedEntities = new();
    public readonly Dictionary<Type, bool> DirtyTrackedEntities = new();

    public int LoadSeed => Name.Aggregate(0, (current, c) => current + c);

    public static readonly HashSet<string> IllegalOptionNames = new(){ "id", "x", "y", "width", "height", "originX", "originY", "nodes" };
    private static readonly Regex tileSplitter = new("\\r\\n|\\n\\r|\\n|\\r");

    internal Room(string name, Rectangle bounds, Map map) {
        Name = name;
        Bounds = bounds;
        Map = map;
        fgTileMap = new VirtualMap<char>(bounds.Width, bounds.Height, '0');
        bgTileMap = new VirtualMap<char>(bounds.Width, bounds.Height, '0');
        Autotile();
    }

    internal Room(LevelData data, Map map)
        : this(data.Name, data.TileBounds, map) {

        // Music
        Music = data.Music;
        AltMusic = data.AltMusic;
        Ambience = data.Ambience;

        MusicLayers = new bool[4];
        MusicLayers[0] = data.MusicLayers[0] > 0;
        MusicLayers[1] = data.MusicLayers[1] > 0;
        MusicLayers[2] = data.MusicLayers[2] > 0;
        MusicLayers[3] = data.MusicLayers[3] > 0;

        MusicProgress = data.MusicProgress;
        AmbienceProgress = data.AmbienceProgress;

        // Camera
        CameraOffset = data.CameraOffset;

        // Misc
        Dark = data.Dark;
        Underwater = data.Underwater;
        Space = data.Space;
        DisableDownTransition = data.DisableDownTransition;
        WindPattern = data.WindPattern;

        // BgTiles
        string[] array = tileSplitter.Split(data.Bg);
        for (int i = 0; i < array.Length; i++)
            for (int j = 0; j < array[i].Length; j++)
                bgTileMap[j, i] = array[i][j];

        // FgTiles
        string[] array2 = tileSplitter.Split(data.Solids);
        for (int i = 0; i < array2.Length; i++)
            for (int j = 0; j < array2[i].Length; j++)
                fgTileMap[j, i] = array2[i][j];

        Autotile();

        // BgDecals
        foreach (DecalData decal in data.BgDecals)
            BgDecals.Add(new Decal(this, decal));

        // FgDecals
        foreach (DecalData decal in data.FgDecals)
            FgDecals.Add(new Decal(this, decal));

        // Entities
        foreach (EntityData entity in data.Entities) {
            AddEntity(Entity.TryCreate(this, entity, false, out bool success));
            if (!success)
                Snowberry.Log(LogLevel.Warn, $"Attempted to load unknown entity ('{entity.Name}'), using placeholder plugin");
        }

        // Player Spawnpoints (excluded from LevelData.Entities)
        foreach (Vector2 spawn in data.Spawns) {
            var entity = Entity.Create("player", this);
            entity.SetPosition(spawn);
            AddEntity(entity);
        }

        // Triggers
        foreach (EntityData trigger in data.Triggers) {
            AddEntity(Entity.TryCreate(this, trigger, true, out bool success));
            if (!success)
                Snowberry.Log(LogLevel.Warn, $"Attempted to load unknown trigger ('{trigger.Name}')");
        }
    }

    public char GetTile(bool fg, Vector2 at) {
        return fg ? GetFgTile(at) : GetBgTile(at);
    }

    public char GetFgTile(Vector2 at) {
        Vector2 p = (at - Position * 8) / 8;
        return fgTileMap[(int)p.X, (int)p.Y];
    }

    public char GetBgTile(Vector2 at) {
        Vector2 p = (at - Position * 8) / 8;
        return bgTileMap[(int)p.X, (int)p.Y];
    }

    public bool SetFgTile(int x, int y, char tile) {
        char orig = fgTileMap[x, y];
        if (orig != tile) {
            fgTileMap[x, y] = tile;
            return true;
        }

        return false;
    }

    public bool SetBgTile(int x, int y, char tile) {
        char orig = bgTileMap[x, y];
        if (orig != tile) {
            bgTileMap[x, y] = tile;
            return true;
        }

        return false;
    }

    public void Autotile() {
        fgTiles = GFX.FGAutotiler.GenerateMap(fgTileMap, new Autotiler.Behaviour { EdgesExtend = true }).TileGrid.Tiles;
        bgTiles = GFX.BGAutotiler.GenerateMap(bgTileMap, new Autotiler.Behaviour { EdgesExtend = true }).TileGrid.Tiles;
    }

    internal List<Selection> GetSelectedObjects(Rectangle rect, bool entities, bool triggers, bool fgDecals, bool bgDecals) {
        List<Selection> result = new List<Selection>();

        if (entities || triggers)
            foreach (Entity entity in AllEntities) {
                if ((!triggers && entity.IsTrigger) || (!entities && !entity.IsTrigger))
                    continue;

                var rects = entity.SelectionRectangles;
                if (rects is { Length: > 0 }) {
                    List<EntitySelection.SelectionRect> selection = new List<EntitySelection.SelectionRect>();
                    bool wasSelected = false;
                    for (int i = 0; i < rects.Length; i++) {
                        Rectangle r = rects[i];
                        if (rect.Intersects(r)) {
                            selection.Add(new EntitySelection.SelectionRect(entity, i - 1));
                            wasSelected = true;
                        }
                    }

                    if (wasSelected)
                        result.Add(new EntitySelection(entity, selection));
                }
            }

        if(fgDecals)
            result.AddRange(
                from fgDecal
                in FgDecals
                where fgDecal.Bounds.Intersects(rect)
                select new DecalSelection(fgDecal, true)
            );

        if(bgDecals)
            result.AddRange(
                from bgDecal
                in BgDecals
                where bgDecal.Bounds.Intersects(rect)
                select new DecalSelection(bgDecal, false)
            );

        return result;
    }

    internal void CalculateScissorRect(Editor.BufferCamera camera) {
        Vector2 offset = Position * 8;

        Vector2 zero = Calc.Round(Vector2.Transform(offset, camera.Matrix));
        Vector2 size = Calc.Round(Vector2.Transform(offset + new Vector2(Width * 8, Height * 8), camera.Matrix) - zero);
        ScissorRect = new Rectangle(
            (int)zero.X, (int)zero.Y,
            (int)size.X, (int)size.Y);
    }

    internal void Render(Rectangle viewRect) {
        Vector2 offset = Position * 8;

        Draw.Rect(offset, Width * 8, Height * 8, Color.White * 0.1f);

        int startX = Math.Max(0, (viewRect.Left - X * 8) / 8);
        int startY = Math.Max(0, (viewRect.Top - Y * 8) / 8);
        int endX = Math.Min(Width, Width + (viewRect.Right - (X + Width) * 8) / 8);
        int endY = Math.Min(Height, Height + (viewRect.Bottom - (Y + Height) * 8) / 8);

        // BgTiles
        for (int x = startX; x < endX; x++)
            for (int y = startY; y < endY; y++)
                if (bgTiles[x, y] != null)
                    bgTiles[x, y].Draw(offset + new Vector2(x, y) * 8);

        // BgDecals
        foreach (Decal decal in BgDecals)
            decal.Render(offset);

        // Entities
        foreach (Entity entity in Entities) {
            Calc.PushRandom(entity.GetHashCode());
            entity.RenderBefore();
            Calc.PopRandom();
        }

        foreach (Entity entity in Entities) {
            Calc.PushRandom(entity.GetHashCode());
            entity.Render();
            Calc.PopRandom();
        }

        // FgTiles
        for (int x = startX; x < endX; x++)
            for (int y = startY; y < endY; y++)
                if (fgTiles[x, y] != null)
                    fgTiles[x, y].Draw(offset + new Vector2(x, y) * 8);

        // FgDecals
        foreach (Decal decal in FgDecals)
            decal.Render(offset);

        // Triggers
        foreach (Entity trigger in Triggers)
            trigger.Render();

        if (this == Editor.SelectedRoom) {
            if (Editor.SelectionInProgress.HasValue)
                Draw.Rect(Editor.SelectionInProgress.Value, Color.Blue * 0.25f);
            if (Editor.SelectedObjects != null)
                foreach (var rect in Editor.SelectedObjects.SelectMany(s => s.Rectangles()))
                    Draw.Rect(rect, Color.Blue * 0.25f);
        }

        DirtyTrackedEntities.Clear();

        foreach (var e in Entities)
            e.Dirty = false;
    }

    internal void HQRender() {
        // Entities
        foreach (Entity entity in Entities)
            entity.HQRender();
        // Triggers
        foreach (Entity trigger in Triggers)
            trigger.HQRender();
        // "See IDs" bind
        if (Editor.Instance.CanTypeShortcut() && MInput.Keyboard.Check(Keys.S) && this == Editor.SelectedRoom && Editor.SelectedObjects != null) {
            foreach (EntitySelection s in Editor.SelectedObjects.OfType<EntitySelection>()) {
                Rectangle mainRect = s.Entity.SelectionRectangles[0];
                string str = $"#{s.Entity.EntityID}";
                Vector2 size = Fonts.Regular.Measure(str) * 0.5f;
                float opacity = mainRect.Contains((int)Editor.Mouse.World.X, (int)Editor.Mouse.World.Y) ? 1 : 0.4f;
                Draw.Rect(new Vector2(mainRect.X, mainRect.Y), size.X + 3, size.Y + 2, Color.Black * opacity);
                Fonts.Regular.Draw(str,  new(mainRect.X + 1, mainRect.Y + 1), new(0.5f), Color.White);
            }
        }
    }

    public void UpdateBounds() {
        var newFgTiles = new VirtualMap<char>(Bounds.Width, Bounds.Height, '0');
        for (int x = 0; x < fgTileMap.Columns; x++)
            for (int y = 0; y < fgTileMap.Rows; y++)
                newFgTiles[x, y] = fgTileMap[x, y];
        fgTileMap = newFgTiles;

        var newBgTiles = new VirtualMap<char>(Bounds.Width, Bounds.Height, '0');
        for (int x = 0; x < bgTileMap.Columns; x++)
            for (int y = 0; y < bgTileMap.Rows; y++)
                newBgTiles[x, y] = bgTileMap[x, y];
        bgTileMap = newBgTiles;

        Autotile();
    }

    public void MoveTiles(int dx, int dy, bool autotile = true) {
        var newFgTiles = new VirtualMap<char>(Bounds.Width, Bounds.Height, '0');
        for (int x = 0; x < fgTileMap.Columns; x++)
            for (int y = 0; y < fgTileMap.Rows; y++)
                newFgTiles[x + dx, y + dy] = fgTileMap[x, y];
        fgTileMap = newFgTiles;

        var newBgTiles = new VirtualMap<char>(Bounds.Width, Bounds.Height, '0');
        for (int x = 0; x < bgTileMap.Columns; x++)
            for (int y = 0; y < bgTileMap.Rows; y++)
                newBgTiles[x + dx, y + dy] = bgTileMap[x, y];
        bgTileMap = newBgTiles;

        if(autotile)
            Autotile();
    }

    public Element CreateLevelData() {
        Element ret = new Element {
            Attributes = new Dictionary<string, object> {
                ["name"] = "lvl_" + Name,
                ["x"] = X * 8,
                ["y"] = Y * 8,
                ["width"] = Width * 8,
                ["height"] = Height * 8,

                ["music"] = Music,
                ["alt_music"] = AltMusic,
                ["ambience"] = Ambience,
                ["musicLayer1"] = MusicLayers[0],
                ["musicLayer2"] = MusicLayers[1],
                ["musicLayer3"] = MusicLayers[2],
                ["musicLayer4"] = MusicLayers[3],

                ["musicProgress"] = MusicProgress,
                ["ambienceProgress"] = AmbienceProgress,

                ["dark"] = Dark,
                ["underwater"] = Underwater,
                ["space"] = Space,
                ["disableDownTransition"] = DisableDownTransition,
                ["windPattern"] = WindPattern.ToString(),

                ["cameraOffsetX"] = CameraOffset.X,
                ["cameraOffsetY"] = CameraOffset.Y
            }
        };

        Element entitiesElement = new Element {
            Attributes = new Dictionary<string, object>(),
            Name = "entities",
            Children = new List<Element>()
        };
        ret.Children = new List<Element> { entitiesElement };

        foreach (var entity in Entities) {
            Element entityElem = new Element {
                Name = entity.Name,
                Children = new List<Element>(),
                Attributes = new Dictionary<string, object> {
                    ["id"] = entity.EntityID,
                    ["x"] = entity.X - X * 8,
                    ["y"] = entity.Y - Y * 8,
                    ["width"] = entity.Width,
                    ["height"] = entity.Height,
                    ["originX"] = entity.Origin.X,
                    ["originY"] = entity.Origin.Y
                }
            };
            entity.SaveAttrs(entityElem);

            foreach (var node in entity.Nodes) {
                Element n = new Element {
                    Attributes = new Dictionary<string, object> {
                        ["x"] = node.X - X * 8,
                        ["y"] = node.Y - Y * 8
                    }
                };
                entityElem.Children.Add(n);
            }

            entitiesElement.Children.Add(entityElem);
        }

        Element triggersElement = new Element {
            Attributes = new Dictionary<string, object>(),
            Name = "triggers",
            Children = new List<Element>()
        };
        ret.Children.Add(triggersElement);

        foreach (var trigger in Triggers) {
            Element triggersElem = new Element {
                Name = trigger.Name,
                Children = new List<Element>(),
                Attributes = new Dictionary<string, object> {
                    ["x"] = trigger.X - X * 8,
                    ["y"] = trigger.Y - Y * 8,
                    ["width"] = trigger.Width,
                    ["height"] = trigger.Height,
                    ["originX"] = trigger.Origin.X,
                    ["originY"] = trigger.Origin.Y
                }
            };
            trigger.SaveAttrs(triggersElem);

            foreach (var node in trigger.Nodes) {
                Element n = new Element {
                    Attributes = new Dictionary<string, object> {
                        ["x"] = node.X - X * 8,
                        ["y"] = node.Y - Y * 8
                    }
                };
                triggersElem.Children.Add(n);
            }

            triggersElement.Children.Add(triggersElem);
        }

        Element fgDecalsElem = new Element();
        fgDecalsElem.Name = "fgdecals";
        fgDecalsElem.Children = new List<Element>();
        ret.Children.Add(fgDecalsElem);
        foreach (var decal in FgDecals) {
            Element decalElem = new Element {
                Attributes = new Dictionary<string, object> {
                    ["x"] = decal.Position.X,
                    ["y"] = decal.Position.Y,
                    ["scaleX"] = decal.Scale.X,
                    ["scaleY"] = decal.Scale.Y,
                    ["texture"] = decal.Texture
                }
            };
            fgDecalsElem.Children.Add(decalElem);
        }

        Element bgDecalsElem = new Element();
        bgDecalsElem.Name = "bgdecals";
        bgDecalsElem.Children = new List<Element>();
        ret.Children.Add(bgDecalsElem);
        foreach (var decal in BgDecals) {
            Element decalElem = new Element {
                Attributes = new Dictionary<string, object> {
                    ["x"] = decal.Position.X,
                    ["y"] = decal.Position.Y,
                    ["scaleX"] = decal.Scale.X,
                    ["scaleY"] = decal.Scale.Y,
                    ["texture"] = decal.Texture
                }
            };
            bgDecalsElem.Children.Add(decalElem);
        }

        StringBuilder fgTiles = new StringBuilder();
        for (int y = 0; y < fgTileMap.Rows; y++) {
            for (int x = 0; x < fgTileMap.Columns; x++) {
                fgTiles.Append(fgTileMap[x, y]);
            }

            fgTiles.Append("\n");
        }

        StringBuilder bgTiles = new StringBuilder();
        for (int y = 0; y < bgTileMap.Rows; y++) {
            for (int x = 0; x < bgTileMap.Columns; x++) {
                bgTiles.Append(bgTileMap[x, y]);
            }

            bgTiles.Append("\n");
        }

        Element fgSolidsElem = new Element {
            Name = "solids",
            Attributes = new Dictionary<string, object> {
                ["innerText"] = fgTiles.ToString()
            }
        };
        ret.Children.Add(fgSolidsElem);

        Element bgSolidsElem = new Element {
            Name = "bg",
            Attributes = new Dictionary<string, object> {
                ["innerText"] = bgTiles.ToString()
            }
        };
        ret.Children.Add(bgSolidsElem);

        return ret;
    }

    public void AddEntity(Entity e) {
        AllEntities.Add(e);
        if (e.IsTrigger)
            Triggers.Add(e);
        else
            Entities.Add(e);
        if (e.Tracked) {
            Type tracking = e.GetType();
            if (!TrackedEntities.ContainsKey(tracking))
                TrackedEntities[tracking] = new List<Entity>();
            TrackedEntities[tracking].Add(e);
        }
    }

    public void RemoveEntity(Entity e) {
        AllEntities.Remove(e);
        Entities.Remove(e);
        Triggers.Remove(e);
        Type tracking = e.GetType();
        if (e.Tracked && TrackedEntities.ContainsKey(tracking)) {
            TrackedEntities[tracking].Remove(e);
            if (TrackedEntities[tracking].Count == 0)
                TrackedEntities.Remove(tracking);
        }
    }

    public void MarkEntityDirty(Entity e) {
        if (e.Tracked)
            DirtyTrackedEntities[e.GetType()] = true;

        e.Dirty = true;
    }

    public bool IsEntityTypeDirty(Type t) {
        return DirtyTrackedEntities.ContainsKey(t) && DirtyTrackedEntities[t];
    }

    public UndoRedo.Snapshotter<List<Entity>> SEntityList() => new(
        () => AllEntities.ToList(),
        es => {
            // very carefully
            // TODO: could be more efficient about dirtying tracked entities; not sure how to do in 1 pass
            foreach(var e in AllEntities.Where(e => e.Tracked))
                DirtyTrackedEntities[e.GetType()] = true;
            AllEntities.Clear();
            Entities.Clear();
            Triggers.Clear();
            TrackedEntities.Clear();
            foreach(var entity in es)
                AddEntity(entity);
        }
    );
}