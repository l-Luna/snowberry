using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Triggers;

[Plugin("blackholeStrengthTrigger")]
public class Plugin_BlackHoleStrengthTrigger : Trigger {
    [Option("strength")] public Celeste.BlackholeBG.Strengths Strength = Celeste.BlackholeBG.Strengths.Mild;

    public override void Render() {
        base.Render();
        var str = $"({Strength})";
        Fonts.Pico8.Draw(str, Center + Vector2.UnitY * 6, Vector2.One, new(0.5f), Color.Black);
    }

    public new static void AddPlacements() {
        Placements.Create("Black Hole Strength Trigger", "blackholeStrengthTrigger");
    }
}