using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Triggers;

[Plugin("noRefillTrigger")]
public class Plugin_NoRefillTrigger : Trigger {
    [Option("state")] public bool State = false;

    public override void Render() {
        base.Render();
        var str = State ? "(on)" : "(off)";
        Fonts.Pico8.Draw(str, Center + Vector2.UnitY * 6, Vector2.One, new(0.5f), Color.Black);
    }

    public new static void AddPlacements() {
        Placements.Create("No Refill Trigger", "noRefillTrigger");
    }
}