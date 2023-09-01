using System;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("wallBooster")]
public class Plugin_WallBooster : Entity {

    [Option("left")] public bool Left = true;
    [Option("notCoreMode")] public bool NotCoreMode = false;

    public override int MinHeight => 8;

    public override void Render() {
        base.Render();

        int leftFacingMod = (Left) ? 1 : -1;
        int leftPosMod = (Left) ? 0 : 8;
        string coreMode = (NotCoreMode) ? "ice" : "fire";

        GFX.Game[$"objects/wallBooster/{coreMode}Top00"].Draw(new Vector2(Position.X + leftPosMod, Position.Y), new Vector2(0, 0), Color.White, new Vector2(leftFacingMod, 1));
        GFX.Game[$"objects/wallBooster/{coreMode}Bottom00"].Draw(new Vector2(Position.X + leftPosMod, Position.Y + Height - 8), new Vector2(0, 0), Color.White, new Vector2(leftFacingMod, 1));
        for (int i = 1; i < (Height - 8) / 8; i++) {
            GFX.Game[$"objects/wallBooster/{coreMode}Mid00"].Draw(new Vector2(Position.X + leftPosMod, (Position.Y + (8 * i))), new Vector2(0, 0), Color.White, new Vector2(leftFacingMod, 1));
        }
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(8, Height));
    }

    public static void AddPlacements() {
        Placements.Create("Wall Booster", "wallBooster");
    }
}