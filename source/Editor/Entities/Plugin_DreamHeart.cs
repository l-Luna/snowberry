using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Entities;

[Plugin("dreamHeartGem")]
public class Plugin_DreamHeart : Entity {

    public override void Render() {
        base.Render();

        FromSprite("heartgem0", "idle")?.DrawCentered(Position);
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(16, 16), justify: new(0.5f, 0.5f));
    }

    public static void AddPlacements() {
        Placements.Create("Crystal Heart (Dream)", "dreamHeartGem");
    }
}