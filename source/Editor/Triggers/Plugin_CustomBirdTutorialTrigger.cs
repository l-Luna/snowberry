using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Triggers;

[Plugin("everest/customBirdTutorialTrigger")]
public class Plugin_CustomBirdTutorialTrigger : Trigger {

    [Option("birdId")] public string BirdID = "";
    [Option("showTutorial")] public bool ShowTutorial = true;

    public override void Render() {
        base.Render();

        var str = (BirdID == "") ? "" : $"({BirdID})";
        Fonts.Pico8.Draw(str, Center + Vector2.UnitY * 6, Vector2.One, new(0.5f), Color.Black);
    }

    public new static void AddPlacements() {
        Placements.Create("Custom Bird Tutorial Trigger (Everest)", "everest/customBirdTutorialTrigger");
    }
}