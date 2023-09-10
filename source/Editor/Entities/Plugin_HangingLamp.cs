using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("hanginglamp")]
public class Plugin_HangingLamp : Entity {

    public override int MinHeight => 16;
    public override void Render() {
        base.Render();

        MTexture lampTex = GFX.Game["objects/hanginglamp"];
        MTexture lampTop = lampTex.GetSubtexture(0, 0, 8, 3);
        MTexture lampTopChain = lampTex.GetSubtexture(0, 10, 8, 6);
        MTexture lampChain = lampTex.GetSubtexture(0, 8, 8, 8);
        MTexture lampLamp = lampTex.GetSubtexture(0, 16, 8, 8);

        lampTop.Draw(Position);
        lampTopChain.Draw(new Vector2(Position.X, Position.Y + 2));
        lampLamp.Draw(new Vector2(Position.X, Position.Y + Height - 8));
        for (int i = 1; i < (Height - 8) / 8; i++) {
            lampChain.Draw(new Vector2(Position.X, (Position.Y + (8 * i))));
        }
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(8, Height));
    }

    public static void AddPlacements() {
        Placements.Create("Hanging Lamp", "hanginglamp");
    }
}