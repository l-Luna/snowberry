using System.Collections.Generic;
using Celeste;
using Monocle;
using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Entities;

[Plugin("fakeHeart")]
public class Plugin_FakeHeart : Entity {

    public static readonly MTexture[] sprites = {
        GFX.Game["collectables/heartGem/0/00"],
        GFX.Game["collectables/heartGem/1/00"],
        GFX.Game["collectables/heartGem/2/00"]
    };

    [Option("color")] public HeartColors Color = HeartColors.Random;

    public override void Render() {
        base.Render();

        (Color switch {
            HeartColors.Normal => sprites[0],
            HeartColors.BSide => sprites[1],
            HeartColors.CSide => sprites[2],
            _ => sprites[Calc.Random.Next(0, 3)]
        }).DrawCentered(Position);
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(18), justify: new(0.5f));
    }

    public static void AddPlacements() {
        Placements.Create("Fake Heart", "fakeHeart");
    }

    public enum HeartColors {
        Normal,
        BSide,
        CSide,
        Random
    }
}