using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("fireBarrier")]
public class Plugin_FireBarrier : Entity {

    private static readonly Color BorderColor = Calc.HexToColor("F66212");
    private static readonly Color InnerColor = Calc.HexToColor("D10901");

    public override int MinWidth => 8;
    public override int MinHeight => 8;

    public override void Render() {
        base.Render();

        Draw.HollowRect(Position, Width, Height, BorderColor);
        Draw.Rect(new(Position.X + 1, Position.Y + 1), Width - 2, Height - 2, InnerColor * 0.4f);
    }

    public static void AddPlacements() {
        Placements.Create("Fire Barrier", "fireBarrier");
    }
}