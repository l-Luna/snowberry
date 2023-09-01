using Microsoft.Xna.Framework;
using Celeste;
using System.Collections.Generic;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("flingBird")]
public class Plugin_FlingBird : Entity {

    [Option("waiting")] public bool Waiting = false;

    public override int MaxNodes => -1;

    public Plugin_FlingBird() {
        Tracked = true;
    }

    public override void Render() {
        base.Render();

        MTexture sprite = GFX.Game["characters/bird/hover04"];
        sprite.DrawCentered(Position);

        foreach (var node in Nodes)
            sprite.DrawCentered(node + new Vector2(1, -4), Color.White * 0.5f);
    }

    public override void HQRender() {
        base.HQRender();

        Vector2 prev = Position;
        foreach (Vector2 node in Nodes) {
            DrawUtil.DottedLine(prev, node, Color.White * 0.5f, 8, 4);
            prev = node;
        }


        if (Room.TrackedEntities[typeof(Plugin_FlingBird)] is { Count: > 1 }) {
            Vector2 startPos = Nodes.Count == 0 ? Position : Nodes[Nodes.Count - 1];
            Vector2? next = NextBirdPos();
            if(next != null)
                DrawUtil.DottedLine(startPos, next.Value, Color.Blue, 8, 4);
        }
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(18, 12), position: new(-1f, 2.5f), justify: new(0.5f, 0.5f));
        foreach (var node in Nodes)
            yield return RectOnAbsolute(new(18, 12), position: node, justify: new(0.5f, 0.5f));
    }

    public static void AddPlacements() {
        Placements.Create("Fling Bird", "flingBird");
    }

    protected Vector2? NextBirdPos() {
        float? minDx = null;
        Vector2? closestPos = null;

        foreach(var entity in Room.TrackedEntities[typeof(Plugin_FlingBird)]){
            if(entity == this)
                continue;

            float dx = entity.Position.X - Position.X;
            if (dx > 0 && (minDx == null || dx < minDx)) {
                closestPos = entity.Position;
                minDx = dx;
            }
        }

        return closestPos;
    }
}