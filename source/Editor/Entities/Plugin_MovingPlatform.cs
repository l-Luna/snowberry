using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("movingPlatform")]
public class Plugin_MovingPlatform : Entity {

    // TODO: suggestions
    [Option("texture")] public string Texture = "default";

    public override int MinWidth => 16;
    public override int MinNodes => 1;
    public override int MaxNodes => 1;

    public override void InitializeAfter() {
        base.InitializeAfter();

        Texture = Plugin_SinkingPlatform.GetVanillaLevelTexture() ?? Texture;
    }

    public override void Render() {
        base.Render();

        KeyValuePair<Color, Color> colours = GetLineColours();
        Color lineEdgeColor = colours.Key, lineInnerColor = colours.Value;

        var end = Nodes[0] + new Vector2(Width / 2f, 4);
        var linePos = Position + new Vector2(Width / 2f, 4);
        Vector2 diff = (end - linePos).SafeNormalize();
        Vector2 diffP = new Vector2(-diff.Y, diff.X);
        Draw.Line(linePos - diff - diffP, end + diff - diffP, lineEdgeColor);
        Draw.Line(linePos - diff, end + diff, lineEdgeColor);
        Draw.Line(linePos - diff + diffP, end + diff + diffP, lineEdgeColor);
        Draw.Line(linePos, end, lineInnerColor);

        Plugin_SinkingPlatform.DrawPlatform(Nodes[0], Width, Texture, Color.White * 0.4f);
        Plugin_SinkingPlatform.DrawPlatform(Position, Width, Texture);
    }

    public static KeyValuePair<Color, Color> GetLineColours() {
        return Editor.Instance.Map.Id.Key()?.ID switch {
            4 => new(Calc.HexToColor("a4464a"), Calc.HexToColor("86354e")),
            _ => new(Calc.HexToColor("2a1923"), Calc.HexToColor("160b12"))
        };
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(Width, 8));
        yield return RectOnAbsolute(new(Width, 8), Nodes[0]);
    }

    public static void AddPlacements() {
        Placements.EntityPlacementProvider.Create("Moving Platform", "movingPlatform");
    }
}