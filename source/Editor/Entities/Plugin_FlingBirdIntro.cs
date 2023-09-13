using Microsoft.Xna.Framework;
using Celeste;
using System.Collections.Generic;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("flingBirdIntro")]
public class Plugin_FlingBirdIntro : Entity {
    [Option("crashes")] public bool Crashes = false;

    public override int MaxNodes => -1;

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
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(18, 12), position: new(-1f, 2.5f), justify: new(0.5f, 0.5f));
        foreach (var node in Nodes)
            yield return RectOnAbsolute(new(18, 12), position: node, justify: new(0.5f, 0.5f));
    }

    public static void AddPlacements() {
        Placements.Create("Fling Bird Intro", "flingBirdIntro");
    }
}