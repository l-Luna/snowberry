using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("sinkingPlatform")]
public class Plugin_SinkingPlatform : Entity {

    public static readonly Color LineEdgeColour = Calc.HexToColor("2a1923");
    public static readonly Color LineInnerColour = Calc.HexToColor("160b12");

    // TODO: suggestions
    [Option("texture")] public string Texture = "default";

    public override int MinWidth => 16;

    public override void InitializeAfter() {
        base.InitializeAfter();

        Texture = GetVanillaLevelTexture() ?? Texture;
    }

    public override void Render() {
        base.Render();

        Draw.Rect(X - 1 + Width / 2f, Y + 4, 3, (Room?.Height ?? 15) * 8, LineEdgeColour);
        Draw.Rect(X + Width / 2f, Y + 4, 1, (Room?.Height ?? 15) * 8, LineInnerColour);

        DrawPlatform(Position, Width, Texture);
    }

    public static void DrawPlatform(Vector2 position, int width, string texture, Color? c = default) {
        Color color = c ?? Color.White;
        MTexture tex = GFX.Game["objects/woodPlatform/" + texture];
        MTexture
            start = tex.GetSubtexture(0, 0, 8, 8),
            mid = tex.GetSubtexture(8, 0, 8, 8),
            centre = tex.GetSubtexture(16, 0, 8, 8),
            end = tex.GetSubtexture(24, 0, 8, 8);

        start.Draw(position, Vector2.Zero, color);
        for(int i = 8; i < width - 8; i += 8)
            mid.Draw(position + new Vector2(i, 0), Vector2.Zero, color);
        end.Draw(position + new Vector2(width - 8, 0), Vector2.Zero, color);
        centre.Draw(position + new Vector2(width / 2f - 4, 0), Vector2.Zero, color);
    }

    public static string GetVanillaLevelTexture() {
        return Editor.Instance.Map.Id.Key()?.ID switch {
            4 => "cliffside",
            _ => null
        };
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(Width, 8));
    }

    public static void AddPlacements() {
        Placements.EntityPlacementProvider.Create("Sinking Platform", "sinkingPlatform");
    }
}