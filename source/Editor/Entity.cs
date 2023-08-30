using System;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;
using Snowberry.Editor.Entities;

namespace Snowberry.Editor;

public abstract class Entity : Plugin {
    public Room Room { get; private set; }

    public int EntityID = 0;

    public Vector2 Position { get; private set; }
    public int X => (int)Position.X;
    public int Y => (int)Position.Y;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Vector2 Origin { get; private set; }
    public virtual bool IsTrigger => false;

    public Rectangle Bounds => new(X, Y, Width, Height);
    public Vector2 Center => Position + new Vector2(Width, Height) / 2f;
    public Vector2 Size => new(Width, Height);

    public bool Tracked { get; protected set; }

    // -1 = not resizable in that direction
    public virtual int MinWidth => -1;
    public virtual int MinHeight => -1;

    public virtual int MinNodes => 0;

    // -1 = unlimited nodes
    public virtual int MaxNodes => 0;

    public bool Dirty = false;

    public readonly List<Vector2> Nodes = new();

    private bool updateSelection = true;
    private Rectangle[] selectionRectangles;

    internal Rectangle[] SelectionRectangles {
        get {
            if (updateSelection) {
                selectionRectangles = Select().ToArray();
                updateSelection = false;
            }

            return selectionRectangles;
        }
    }

    public Entity SetPosition(Vector2 position) {
        if(position != Position)
            Room?.MarkEntityDirty(this);

        Position = position;
        updateSelection = true;
        return this;
    }

    public void Move(Vector2 amount) {
        if(amount != Vector2.Zero)
            Room?.MarkEntityDirty(this);

        Position += amount;
        updateSelection = true;
    }

    public Vector2? GetNode(int i) {
        if (i >= 0 && i < Nodes.Count) {
            return Nodes[i];
        }
        return null;
    }

    public void SetNode(int i, Vector2 position) {
        if (i >= 0 && i < Nodes.Count) {
            if(Nodes[i] != position)
                Room?.MarkEntityDirty(this);

            Nodes[i] = position;
            updateSelection = true;
        }
    }

    public void MoveNode(int i, Vector2 amount) {
        if (i >= 0 && i < Nodes.Count) {
            if(amount != Vector2.Zero)
                Room?.MarkEntityDirty(this);
            Nodes[i] += amount;
            updateSelection = true;
        }

        Room?.MarkEntityDirty(this);
    }

    public void AddNode(Vector2 position, int? idx = null) {
        if (idx == null)
            Nodes.Add(position);
        else
            Nodes.Insert(idx.Value, position);

        updateSelection = true;
        Room?.MarkEntityDirty(this);
    }

    internal void ResetNodes() {
        updateSelection = true;
        Nodes.Clear();
        Room?.MarkEntityDirty(this);
    }

    public virtual void SetWidth(int width) {
        Width = width;
        updateSelection = true;
        Room?.MarkEntityDirty(this);
    }

    public virtual void SetHeight(int height) {
        Height = height;
        updateSelection = true;
        Room?.MarkEntityDirty(this);
    }

    public override void Set(string option, object value) {
        base.Set(option, value);
        updateSelection = true;
    }

    public virtual void ChangeDefault() { }

    public virtual void Initialize() {
        ChangeDefault();
    }

    public virtual void InitializeAfter() { }

    public virtual void SaveAttrs(BinaryPacker.Element e) {
        foreach (var opt in Info.Options.Keys) {
            var val = Get(opt);
            // check that we don't overwrite any of the above (e.g. from a LuaEntity)
            if(val != null && !Room.IllegalOptionNames.Contains(opt))
                e.Attributes[opt] = val;
        }
    }

    protected virtual IEnumerable<Rectangle> Select() {
        List<Rectangle> ret = new List<Rectangle> {
            new(Width < 6 ? X - 3 : X, Height < 6 ? Y - 3 : Y, Width < 6 ? 6 : Width, Height < 6 ? 6 : Height)
        };
        ret.AddRange(Nodes.Select(node => new Rectangle((int)node.X - 3, (int)node.Y - 3, 6, 6)));

        return ret.ToArray();
    }

    public static Rectangle RectOnAbsolute(Vector2 size, Vector2 position = default, Vector2 justify = default) {
        return new Rectangle(
            (int)(position.X - justify.X * size.X),
            (int)(position.Y - justify.Y * size.Y),
            (int)size.X,
            (int)size.Y
        );
    }

    public Rectangle RectOnRelative(Vector2 size, Vector2 position = default, Vector2 justify = default) {
        return RectOnAbsolute(size, Position + position, justify);
    }

    public virtual void Render() { }
    public virtual void RenderBefore() { }
    public virtual void HQRender() { }

    private static readonly Sprite carrier = new(GFX.SpriteBank.Atlas, "strawberry");

    public static MTexture FromSprite(string spriteName, string animName) {
        try{
            GFX.SpriteBank.CreateOn(carrier, spriteName);
            carrier.Play(animName);
            return carrier.Texture;
        }catch (Exception){
            return null;
        }
    }

    #region Entity Instantiating

    public virtual void ApplyDefaults() { }

    protected virtual Entity InitializeData(EntityData entityData) {
        Vector2 offset = Room.Position * 8;

        Position = entityData.Position + offset;
        Width = entityData.Width;
        Height = entityData.Height;
        Origin = entityData.Origin;
        EntityID = entityData.ID;

        Nodes.Clear();
        foreach (Vector2 node in entityData.Nodes)
            Nodes.Add(node + offset);

        return InitializeData(entityData.Values);
    }

    protected virtual Entity InitializeData(Dictionary<string, object> data) {
        if (data != null)
            foreach (KeyValuePair<string, object> pair in data)
                Set(pair.Key, pair.Value);

        Initialize();
        return this;
    }

    public static Entity Create(string name, Room room) {
        if (PluginInfo.Entities.TryGetValue(name, out PluginInfo plugin)) {
            Entity entity = plugin.Instantiate<Entity>();

            entity.Room = room;

            entity.ApplyDefaults();
            entity.Initialize();
            return entity;
        }

        return null;
    }

    internal static Entity Create(Room room, EntityData entityData) {
        if (PluginInfo.Entities.TryGetValue(entityData.Name, out PluginInfo plugin)) {
            Entity entity = plugin.Instantiate<Entity>();

            entity.Room = room;

            return entity.InitializeData(entityData);
        }

        return null;
    }

    internal static Entity TryCreate(Room room, EntityData entityData, bool trigger, out bool success) {
        Entity entity = Create(room, entityData);

        if (entity == null) {
            var name = entityData.Name;
            entity = new UnknownEntity {
                Room = room,
                LoadedFromTrigger = trigger,
                Info = new UnknownPluginInfo(name, entityData.Values),
                Name = name
            };
            entity.InitializeData(entityData);
        }

        success = entity is not UnknownEntity;

        return entity;
    }

    #endregion
}