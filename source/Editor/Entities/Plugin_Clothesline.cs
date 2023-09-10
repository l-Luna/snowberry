using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.Editor.Entities.Util;

namespace Snowberry.Editor.Entities;

[Plugin("clothesline")]
public class Plugin_Clothesline : Entity {

    public static readonly Color[] Colours = {
        Calc.HexToColor("0d2e6b"),
        Calc.HexToColor("3d2688"),
        Calc.HexToColor("4f6e9d"),
        Calc.HexToColor("47194a")
    };
    public static readonly Color LineColour = Color.Lerp(Color.Gray, Color.DarkBlue, 0.25f);
    public static readonly Color PinColour = Color.Gray;

    public override int MinNodes => 1;
    public override int MaxNodes => 1;

    private readonly EditorFlagline flagline;

    public Plugin_Clothesline() {
        flagline = new EditorFlagline(Position, Position, LineColour, PinColour, Colours, 8, 20, 8, 16, 2, 8);
    }

    public override void Render() {
        base.Render();
        flagline.From = Position;
        flagline.To = Nodes[0];
        flagline.Render();
    }

    public static void AddPlacements() {
        Placements.Create("Clothesline", "clothesline");
    }
}