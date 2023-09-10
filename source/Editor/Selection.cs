using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Snowberry.Editor;

public abstract class Selection {

    // e.g. entity type or decal path, should be unique
    public abstract string Name();

    public abstract Color Accent();

    public abstract bool Contains(Point p);

    public abstract void Move(Vector2 amount);

    public abstract void RemoveSelf();

    public abstract IEnumerable<Rectangle> Rectangles();

    public abstract UndoRedo.Snapshotter SPositions();
}

public class EntitySelection : Selection {

    public class SelectionRect {
        public readonly int Index;
        private readonly Entity entity;

        public SelectionRect(Entity entity, int i) {
            Index = i;
            this.entity = entity;
        }

        // -1 = entity itself
        public Rectangle Rect => entity.SelectionRectangles.Length > Index + 1 ? entity.SelectionRectangles[Index + 1] : Rectangle.Empty;
    }

    public readonly Entity Entity;
    public readonly List<SelectionRect> Selections;

    public EntitySelection(Entity entity, List<SelectionRect> selection) {
        Entity = entity;
        Selections = selection;
    }

    public override string Name() => Entity.Name;

    public override Color Accent() => Entity.Info.Module.Color;

    public override bool Contains(Point p) =>
        Selections.Any(s => s.Rect.Contains(p));

    public override void Move(Vector2 amount) {
        foreach (SelectionRect s in Selections)
            if (s.Index < 0)
                Entity.Move(amount);
            else
                Entity.MoveNode(s.Index, amount);
    }

    /*public void SetPosition(Vector2 position, int i) {
        if (i < 0)
            Entity.SetPosition(position);
        else
            Entity.SetNode(i, position);
    }*/

    public void SetWidth(int width) => Entity.SetWidth(width);

    public void SetHeight(int height) => Entity.SetHeight(height);

    public override void RemoveSelf() {
        Entity.Room.RemoveEntity(Entity);
        Entity.Room.MarkEntityDirty(Entity); // tracked entities
    }

    public override IEnumerable<Rectangle> Rectangles() => Selections.Select(r => r.Rect);

    public override UndoRedo.Snapshotter SPositions() => Entity.SPosition().And(Entity.SNodes());

    public override bool Equals(object obj) =>
        obj is EntitySelection s && s.Entity.Equals(Entity) && s.Selections.All(it => Selections.Any(x => x.Index == it.Index));

    public override int GetHashCode() =>
        Entity.GetHashCode() ^ Selections.GetHashCode();
}

public class DecalSelection : Selection {

    public readonly Decal Decal;
    public readonly bool Fg;

    public DecalSelection(Decal decal, bool fg) {
        Decal = decal;
        Fg = fg;
    }

    public override string Name() => Decal.Texture;

    public override Color Accent() => Color.White;

    public override bool Contains(Point p) => Decal.Bounds.Contains(p);

    public override void Move(Vector2 amount) => Decal.Position += amount;

    public override void RemoveSelf() {
        if (Fg)
            Decal.Room.FgDecals.Remove(Decal);
        else
            Decal.Room.BgDecals.Remove(Decal);
    }

    public override IEnumerable<Rectangle> Rectangles() => new[] { Decal.Bounds };

    public override UndoRedo.Snapshotter SPositions() => Decal.SPosition();
}