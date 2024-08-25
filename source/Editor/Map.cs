using Celeste;
using Celeste.Mod;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Snowberry.Editor;

using Element = BinaryPacker.Element;

public class Map {

    public static Dictionary<string, int> MissingObjectReports = new();

    public MapId Id;
    public readonly Element FromRaw;

    public readonly MapMeta Meta;

    public readonly List<Room> Rooms = [];
    public readonly List<Rectangle> Fillers = [];

    public readonly List<Styleground> FGStylegrounds = [];
    public readonly List<Styleground> BGStylegrounds = [];
    public Color BackgroundColor = Color.Black;

    internal Map(MapId id) {
        Id = id;
        Meta = new MapMeta {
            Modes = new MapMetaModeProperties[3]
        };
        for (int i = 0; i < Meta.Modes.Length; i++)
            Meta.Modes[i] ??= new();
        // all other defaults are the same as "leave it unset" except this one }:/
        Meta.Modes[0].SeekerSlowdown = true; // TODO: set other defaults

        SetupGraphics(Meta);
        Tileset.Load();
    }

    internal Map(Element mapElement, MapId id) : this(id) {
        FromRaw = mapElement;

        MissingObjectReports = new();

        if (mapElement.Find("meta") is {} metaElem) {
            Meta = new MapMeta(metaElem);
            if (Meta.Modes.Length < 3) {
                var tmp = Meta.Modes; // thank you C#, very cool
                Array.Resize(ref tmp, 3);
                Meta.Modes = tmp;
            }
            for (int i = 0; i < Meta.Modes.Length; i++)
                Meta.Modes[i] ??= new();
            if (metaElem.Find("mode") is {} modeElem)
                // handled separately in MapData by everest
                Meta.Modes[0].Parse(modeElem);
            Meta.Modes[0].SeekerSlowdown ??= true; // see above
        }

        // Editor.CopyAreaData(targetData, playtestData);
        SetupGraphics(Meta);
        Tileset.Load();

        foreach (Element roomData in mapElement.FindOrEmpty("levels").Children)
            Rooms.Add(new Room(new LevelData(roomData), this));
        foreach (Element filler in mapElement.FindOrEmpty("Filler").Children ?? [])
            Fillers.Add(new Rectangle(
                (int)filler.Attributes["x"],
                (int)filler.Attributes["y"],
                (int)filler.Attributes["w"],
                (int)filler.Attributes["h"]
            ));

        Element styleElement = mapElement.FindOrEmpty("Style");

        // load stylegrounds in reverse, to match saving in reverse
        // keeps an internal representation where smaller indexes (closer to 0) = closer to foreground
        if (styleElement.Find("Foregrounds") is {} foregrounds) {
            foreach (var item in foregrounds.Children.AsEnumerable().Reverse()) {
                string stylename = item.Name;

                if (stylename.ToLowerInvariant().Equals("apply")) {
                    if (item.Children != null) {
                        foreach (var child in item.Children.AsEnumerable().Reverse()) {
                            Styleground styleground = Styleground.Create(child.Name, this, child, item);
                            FGStylegrounds.Add(styleground);
                        }
                    }
                } else {
                    Styleground styleground = Styleground.Create(stylename, this, item);
                    FGStylegrounds.Add(styleground);
                }
            }
        }

        if (styleElement.Find("Backgrounds") is {} backgrounds) {
            foreach (var item in backgrounds.Children.AsEnumerable().Reverse()) {
                string stylename = item.Name;

                if (stylename.ToLowerInvariant().Equals("apply")) {
                    if (item.Children != null) {
                        foreach (var child in item.Children.AsEnumerable().Reverse()) {
                            Styleground styleground = Styleground.Create(child.Name, this, child, item);
                            BGStylegrounds.Add(styleground);
                        }
                    }
                } else {
                    Styleground styleground = Styleground.Create(stylename, this, item);
                    BGStylegrounds.Add(styleground);
                }
            }
        }

        if (styleElement.HasAttr("color"))
            BackgroundColor = Calc.HexToColor(styleElement.Attr("color"));

        Rooms.ForEach(r => r.AllEntities.ForEach(e => e.InitializeAfter()));

        Snowberry.Log(LogLevel.Info, $"Loaded {FGStylegrounds.Count} foreground and {BGStylegrounds.Count} background stylegrounds.");
        foreach (var (objName, quantity) in MissingObjectReports)
            if (quantity > 0)
                Snowberry.Log(LogLevel.Warn, $"Attempted to load unknown object ('{objName}') x{quantity}, using placeholder plugin");
        MissingObjectReports = new();
    }

    public static Map FromData(MapData data) {
        Element e = BinaryPacker.FromBinary(data.Filepath);
        MapId id = new LoadedMapId(data.Data.ToKey());
        // if the path is null, then the map is part of a zip/vanilla and should be locked instead
        if(id.Path() == null)
            id = new LockedMapId(data.Data.ToKey(), DateTime.Now);
        return e == null ? null : new Map(e, id);
    }

    internal Room GetRoomAt(Point at) => Rooms.FirstOrDefault(room => new Rectangle(room.X * 8, room.Y * 8, room.Width * 8, room.Height * 8).Contains(at));

    internal int GetFillerIndexAt(Point at) {
        for (int i = 0; i < Fillers.Count; i++) {
            Rectangle filler = Fillers[i];
            if (new Rectangle(filler.X * 8, filler.Y * 8, filler.Width * 8, filler.Height * 8).Contains(at))
                return i;
        }

        return -1;
    }

    internal void Render(Editor.BufferCamera camera) {
        Rectangle viewRect = camera.ViewRect;

        List<Room> visibleRooms = [];
        foreach (Room room in Rooms) {
            Rectangle rect = new Rectangle(room.Bounds.X * 8, room.Bounds.Y * 8, room.Bounds.Width * 8, room.Bounds.Height * 8);
            if (viewRect.Intersects(rect)) {
                room.CalculateScissorRect(camera);
                visibleRooms.Add(room);
            }
        }

        // render stylegrounds in correct order; 0 = top
        if (Editor.StylegroundsPreviews)
            foreach (var styleground in BGStylegrounds.AsEnumerable().Reverse())
                foreach (Room room in visibleRooms.Where(styleground.IsVisible))
                    DrawUtil.WithinScissorRectangle(room.ScissorRect, () => styleground.Render(room), camera.Matrix, nested: false, styleground.Additive);

        foreach (Room room in visibleRooms)
            DrawUtil.WithinScissorRectangle(room.ScissorRect, () => room.Render(viewRect), camera.Matrix, nested: false);

        if (Editor.StylegroundsPreviews)
            foreach (var styleground in FGStylegrounds.AsEnumerable().Reverse())
                foreach (var room in visibleRooms.Where(styleground.IsVisible))
                    DrawUtil.WithinScissorRectangle(room.ScissorRect, () => styleground.Render(room), camera.Matrix, nested: false, styleground.Additive);

        // render gray over non-selected rooms, over FG stylegrounds
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix);
        foreach (var room in visibleRooms.Where(room => room != Editor.SelectedRoom))
            Draw.Rect(room.Position * 8, room.Width * 8, room.Height * 8, Color.Black * 0.5f);
        Draw.SpriteBatch.End();

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix);
        for (int i = 0; i < Fillers.Count; i++) {
            Rectangle filler = Fillers[i];
            Rectangle rect = new Rectangle(filler.X * 8, filler.Y * 8, filler.Width * 8, filler.Height * 8);
            Draw.Rect(rect, Color.White * (Editor.SelectedFillerIndex == i ? 0.14f : 0.1f));
        }

        Draw.SpriteBatch.End();
    }

    internal void PostRender() {
        foreach (var room in Rooms)
            room.PostRender();
    }

    internal void HQRender(Editor.BufferCamera camera) {
        Rectangle viewRect = camera.ViewRect;

        foreach (Room room in Rooms) {
            Rectangle rect = new Rectangle(room.Bounds.X * 8, room.Bounds.Y * 8, room.Bounds.Width * 8, room.Bounds.Height * 8);
            if (!viewRect.Intersects(rect))
                continue;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Engine.Instance.GraphicsDevice.RasterizerState, null, camera.ScreenView);
            room.HQRender();
            Draw.SpriteBatch.End();
            //DrawUtil.WithinScissorRectangle(room.ScissorRect, room.HQRender, camera.ScreenView, nested: false, false);
        }
    }

    public Element Export(){
        Element map = new Element{
            Children = [],
            Package = "" // still has to be non-null, even though it's completely unused
        };

        // children:
        //   levels w/ levels as children
        Element levels = new Element{
            Name = "levels",
            Children = []
        };
        foreach(var room in Rooms)
            levels.Children.Add(room.CreateLevelData());
        map.Children.Add(levels);

        //   Filler w/ children w/ x,y,w,h
        Element fillers = new Element{
            Name = "Filler",
            Children = []
        };
        foreach(var filler in Fillers)
            fillers.Children.Add(new Element{
                Attributes = new Dictionary<string, object>{
                    ["x"] = filler.X,
                    ["y"] = filler.Y,
                    ["w"] = filler.Width,
                    ["h"] = filler.Height
                }
            });

        map.Children.Add(fillers);

        //   style: w/ optional color, Backgrounds child & Foregrounds child
        Element style = new Element{
            Name = "Style",
            Attributes = new(),
            Children = []
        };

        Element fgStyles = GenerateStylegroundsElement(false);
        Element bgStyles = GenerateStylegroundsElement(true);

        style.Children.Add(fgStyles);
        style.Children.Add(bgStyles);
        map.Children.Add(style);

        //   meta
        Element meta = new Element{
            Name = "meta",
            Attributes = new(),
            Children = []
        };
        meta.SetAttrNn("Parent", Meta.Parent);
        meta.SetAttrNn("Icon", Meta.Icon);
        meta.SetAttrNn("Interlude", Meta.Interlude);
        meta.SetAttrNn("CassetteCheckpointIndex", Meta.CassetteCheckpointIndex);
        meta.SetAttrNn("TitleBaseColor", Meta.TitleBaseColor);
        meta.SetAttrNn("TitleAccentColor", Meta.TitleAccentColor);
        meta.SetAttrNn("TitleTextColor", Meta.TitleTextColor);
        meta.SetAttrNn("IntroType", Meta.IntroType);
        meta.SetAttrNn("Dreaming", Meta.Dreaming);
        meta.SetAttrNn("ColorGrade", Meta.ColorGrade);
        meta.SetAttrNn("Wipe", Meta.Wipe);
        meta.SetAttrNn("DarknessAlpha", Meta.DarknessAlpha);
        meta.SetAttrNn("BloomBase", Meta.BloomBase);
        meta.SetAttrNn("BloomStrength", Meta.BloomStrength);
        meta.SetAttrNn("Jumpthru", Meta.Jumpthru);
        meta.SetAttrNn("CoreMode", Meta.CoreMode);
        meta.SetAttrNn("CassetteNoteColor", Meta.CassetteNoteColor);
        meta.SetAttrNn("CassetteSong", Meta.CassetteSong);
        meta.SetAttrNn("PostcardSoundID", Meta.PostcardSoundID);
        meta.SetAttrNn("ForegroundTiles", Meta.ForegroundTiles);
        meta.SetAttrNn("BackgroundTiles", Meta.BackgroundTiles);
        meta.SetAttrNn("AnimatedTiles", Meta.AnimatedTiles);
        meta.SetAttrNn("Sprites", Meta.Sprites);
        meta.SetAttrNn("Portraits", Meta.Portraits);
        meta.SetAttrNn("OverrideASideMeta", Meta.OverrideASideMeta);
        if(Meta.CassetteModifier != null){
            MapMetaCassetteModifier mod = Meta.CassetteModifier;
            Element cm = new Element{
                Name = "cassettemodifier",
                Children = []
            };

            cm.SetAttr("TempoMult", mod.TempoMult);
            cm.SetAttr("LeadBeats", mod.LeadBeats);
            cm.SetAttr("BeatsPerTick", mod.BeatsPerTick);
            cm.SetAttr("TicksPerSwap", mod.TicksPerSwap);
            cm.SetAttr("Blocks", mod.Blocks);
            cm.SetAttr("BeatsMax", mod.BeatsMax);
            cm.SetAttr("BeatIndexOffset", mod.BeatIndexOffset);
            cm.SetAttr("OldBehavior", mod.OldBehavior);

            meta.Children.Add(cm);
        }

        Element mode = new() {
            Name = "mode",
            Children = []
        };
        var modeProp = Meta.Modes[0];
        mode.SetAttrNn("IgnoreLevelAudioLayerData", modeProp.IgnoreLevelAudioLayerData);
        mode.SetAttrNn("Inventory", modeProp.Inventory);
        mode.SetAttrNn("PoemID", modeProp.PoemID);
        mode.SetAttrNn("StartLevel", modeProp.StartLevel);
        mode.SetAttrNn("HeartIsEnd", modeProp.HeartIsEnd);
        mode.SetAttrNn("SeekerSlowdown", modeProp.SeekerSlowdown);
        mode.SetAttrNn("TheoInBubble", modeProp.TheoInBubble);
        meta.Children.Add(mode);

        map.Children.Add(meta);

        return map;
    }

    private Element GenerateStylegroundsElement(bool bg){
        Element styles = new Element{
            Name = bg ? "Backgrounds" : "Foregrounds",
            Children = []
        };

        // save elements in reverse, to match loading in reverse
        // keeps an internal representation where smaller indexes (closer to 0) = closer to foreground
        foreach(var styleground in (bg ? BGStylegrounds : FGStylegrounds).AsEnumerable().Reverse()){
            Element elem = new Element {
                Name = styleground.Name,
                Attributes = new Dictionary<string, object> {
                    ["tag"] = styleground.Tags,
                    ["x"] = styleground.Position.X,
                    ["y"] = styleground.Position.Y,
                    ["scrollx"] = styleground.Scroll.X,
                    ["scrolly"] = styleground.Scroll.Y,
                    ["speedx"] = styleground.Speed.X,
                    ["speedy"] = styleground.Speed.Y,
                    ["color"] = styleground.Color.IntoRgbString(),
                    ["alpha"] = styleground.Alpha,
                    ["flipx"] = styleground.FlipX,
                    ["flipy"] = styleground.FlipY,
                    ["loopx"] = styleground.LoopX,
                    ["loopy"] = styleground.LoopY,
                    ["wind"] = styleground.WindMultiplier,
                    ["exclude"] = styleground.ExcludeFrom,
                    ["only"] = styleground.OnlyIn,
                    ["flag"] = styleground.Flag,
                    ["notflag"] = styleground.NotFlag,
                    ["always"] = styleground.ForceFlag,
                    ["instantIn"] = styleground.InstantIn,
                    ["instantOut"] = styleground.InstantOut,
                }
            };

            if(styleground.FadeX != null)
                elem.Attributes["fadex"] = styleground.FadeX;

            if(styleground.FadeY != null)
                elem.Attributes["fadey"] = styleground.FadeY;

            if(styleground.DreamingOnly.HasValue)
                elem.Attributes["dreaming"] = styleground.DreamingOnly.Value;

            foreach(var opt in styleground.Info.Options.Keys)
                if (styleground.Get(opt) is {} val)
                    elem.Attributes[opt] = val;
            foreach (var (option, value) in styleground.UnknownAttrs)
                if (value != null)
                    elem.Attributes.TryAdd(option, Plugin.ObjectToStr(value));

            styles.Children.Add(elem);
        }

        return styles;
    }

    // Setup autotilers, animated tiles, and the Graphics atlas, based on LevelLoader
    private void SetupGraphics(MapMeta meta){
        string text = meta?.BackgroundTiles;
        if (string.IsNullOrEmpty(text))
            text = Path.Combine("Graphics", "BackgroundTiles.xml");
        GFX.BGAutotiler = new Autotiler(text);

        text = meta?.ForegroundTiles;
        if (string.IsNullOrEmpty(text))
            text = Path.Combine("Graphics", "ForegroundTiles.xml");
        GFX.FGAutotiler = new Autotiler(text);

        text = meta?.AnimatedTiles;
        if (string.IsNullOrEmpty(text))
            text = Path.Combine("Graphics", "AnimatedTiles.xml");
        GFX.AnimatedTilesBank = new AnimatedTilesBank();
        foreach (XmlElement item in Calc.LoadContentXML(text).GetElementsByTagName("sprite"))
            if (item != null)
                GFX.AnimatedTilesBank.Add(item.Attr("name"), item.AttrFloat("delay", 0.0f), item.AttrVector2("posX", "posY", Vector2.Zero), item.AttrVector2("origX", "origY", Vector2.Zero), GFX.Game.GetAtlasSubtextures(item.Attr("path")));

        GFX.SpriteBank = new SpriteBank(GFX.Game, Path.Combine("Graphics", "Sprites.xml"));
        text = meta?.Sprites;
        if (!string.IsNullOrEmpty(text)) {
            SpriteBank spriteBank = GFX.SpriteBank;
            foreach ((string key, SpriteData value) in new SpriteBank(GFX.Game, GetSanitized(text)).SpriteData) {
                if (spriteBank.SpriteData.TryGetValue(key, out SpriteData value2)) {
                    IDictionary animations = value2.Sprite.Animations;
                    foreach (DictionaryEntry item2 in (IDictionary)value.Sprite.Animations)
                        animations[item2.Key] = item2.Value;

                    value2.Sources.AddRange(value.Sources);
                    value2.Sprite.Stop();
                    if (value.Sprite.CurrentAnimationID != "")
                        value2.Sprite.Play(value.Sprite.CurrentAnimationID);
                } else
                    spriteBank.SpriteData[key] = value;
            }
        }

        SmhInterop.LoadGraphics();
    }

    private XmlDocument GetSanitized(string path) {
        var getSanitizedMInfo = typeof(SpriteBank).GetMethod("GetSpriteBankExcludingVanillaCopyPastes", BindingFlags.Static | BindingFlags.NonPublic);
        return (XmlDocument)getSanitizedMInfo.Invoke(null, [
            Calc.orig_LoadContentXML(Path.Combine("Graphics", "Sprites.xml")), Calc.LoadContentXML(path), path
        ]);
    }
}

internal static class ElementExt{

    internal static void SetAttrNn(this Element e, string attrName, object value) {
        if(value != null)
            (e.Attributes ??= new())[attrName] = value;
    }
}