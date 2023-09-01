using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.Editor.Entities.Util;
using System;
using System.Collections.Generic;
using static Celeste.ZipMover;

namespace Snowberry.Editor.Entities;

[Plugin("zipMover")]
public class Plugin_ZipMover : Entity {
    private static readonly Color ropeColor = Calc.HexToColor("663931");

    [Option("theme")] public Themes Theme = Themes.Normal;

    public override int MinWidth => 16;
    public override int MinHeight => 16;
    public override int MinNodes => 1;
    public override int MaxNodes => 1;

    public override void Render() {
        base.Render();

        MTexture block, light, cog;
        string innercog;
        bool outline;

        string subfolder = (Theme == Themes.Moon) ? "/moon" : "";
        block = GFX.Game[$"objects/zipmover{subfolder}/block"];
        light = GFX.Game[$"objects/zipmover{subfolder}/light01"];
        cog = GFX.Game[$"objects/zipmover{subfolder}/cog"];
        innercog = $"objects/zipmover{subfolder}/innercog";
        outline = (Theme == Themes.Moon) ? false : true;

        // Draw path
        Vector2 start = Center;
        Vector2 end = Nodes[0] + new Vector2(Width, Height) / 2f;

        if (start != end) {
            Vector2 perp = (end - start).SafeNormalize().Perpendicular();
            Vector2 a = perp * 3f;
            Vector2 b = -perp * 4f;
            Draw.Line(start + a, end + a, ropeColor);
            Draw.Line(start + b, end + b, ropeColor);
        }

        cog.DrawCentered(end);

        drawBlock(Nodes[0], 0.15f);
        drawBlock(Position, 1);

        void drawBlock(Vector2 position, float opacity) {
            // Draw black background
            if (outline)
                Draw.Rect(position.X - 1, position.Y - 1, Width + 2, Height + 2, Color.Black * opacity);
            else
                Draw.Rect(position.X + 1, position.Y + 1, Width - 2, Height - 2, Color.Black * opacity);

            // Draw inner cogs
            var innerCogs = GFX.Game.GetAtlasSubtextures(innercog);
            int fg = 1;
            float rotation = 0;
            MTexture temp = new MTexture();
            for (int y = 4; y <= Height - 4f; y += 8) {
                int odd = fg;
                for (int x = 4; x <= Width - 4f; x += 8) {
                    int index = (int)((rotation / ((float)Math.PI / 2f) % 1f) * innerCogs.Count);
                    MTexture iCog = innerCogs[index];
                    Rectangle bounds = new Rectangle(0, 0, iCog.Width, iCog.Height);
                    Vector2 innerbounds = Vector2.Zero;
                    if (x <= 4) {
                        innerbounds.X = 2f;
                        bounds.X = 2;
                        bounds.Width -= 2;
                    } else if (x >= Width - 4f) {
                        innerbounds.X = -2f;
                        bounds.Width -= 2;
                    }

                    if (y <= 4) {
                        innerbounds.Y = 2f;
                        bounds.Y = 2;
                        bounds.Height -= 2;
                    } else if (y >= Height - 4f) {
                        innerbounds.Y = -2f;
                        bounds.Height -= 2;
                    }

                    iCog = iCog.GetSubtexture(bounds.X, bounds.Y, bounds.Width, bounds.Height, temp);
                    iCog.DrawCentered(position + new Vector2(x, y) + innerbounds, Color.White * ((fg < 0) ? 0.5f : 1f) * opacity);
                    fg = -fg;
                    rotation += (float)Math.PI / 3f;
                }

                if (odd == fg) {
                    fg = -fg;
                }
            }

            // Draw box
            EditorNinePatch blockDrawer = new(block);
            blockDrawer.Draw(position, Width, Height, Color.White * opacity);

            // Draw lights
            light.DrawJustified(position + Vector2.UnitX * Width / 2f, new Vector2(0.5f, 0.0f));
        }
    }


    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new Vector2(Width, Height));

        yield return RectOnAbsolute(new(Width, Height), position: Nodes[0]);
    }

    public static void AddPlacements() {
        Placements.Create("Zip Mover (Normal)", "zipMover");
        Placements.Create("Zip Mover (Moon)", "zipMover", new Dictionary<string, object>() { { "theme", "Moon" } });
    }
}