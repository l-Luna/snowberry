using Celeste;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Entities;

[Plugin("lightningBlock")]
public class Plugin_LightningBreakerBox : Entity {
    [Option("flipX")] public bool FlipX = false;
    [Option("musicProgress")] public int MusicProgress = -1;
    [Option("musicSession")] public bool MusicSession = false;
    [Option("music")] public string Music = "";
    [Option("flag")] public bool Flag = false;

    public override void Render() {
        base.Render();

        int facing = FlipX ? -1 : 1;
        GFX.Game["objects/breakerBox/idle00"].DrawCentered(Position + new Vector2(16), Color.White, new Vector2(facing, 1));
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(34));
    }

    public static void AddPlacements() {
        Placements.Create("Lightning Breaker Box", "lightningBlock");
    }
}