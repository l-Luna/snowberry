using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Snowberry.Editor.Entities;

[Plugin("crumbleBlock")]
public class Plugin_CrumbleBlock : Entity {
    [Option("texture")] public string Texture = null;

    public override int MinWidth => 8;

    public override void Render() {
        base.Render();

        // TODO: custom textures
        AreaKey? key = Editor.Instance.Map.Id.Key();
        string suffix = !string.IsNullOrEmpty(Texture) ? Texture : key is {} k ? AreaData.Get(k).CrumbleBlock : "default";
        MTexture mTexture2 = GFX.Game["objects/crumbleBlock/" + suffix];

        for (int j = 0; (float)j < Width; j += 8) {
            int num2 = (int)((Math.Abs(X) + (float)j) / 8f) % 4;
            mTexture2.GetSubtexture(num2 * 8, 0, 8, 8).DrawCentered(new Vector2(4 + j + X, 4f + Y));
        }
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(Width, 8));
    }

    public static void AddPlacements() {
        Placements.EntityPlacementProvider.Create("Crumble Blocks", "crumbleBlock");
    }
}