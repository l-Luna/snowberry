using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Entities;

[Plugin("plateau")]
public class Plugin_Plateau : Entity {

    public override void Render() {
        base.Render();
        
        GFX.Game["scenery/fallplateau"].DrawCentered(Position);
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(120, 16), justify: new(0.5f, 0.5f));
    }

    public static void AddPlacements() {
        Placements.Create("Plateau", "plateau");
    }
}