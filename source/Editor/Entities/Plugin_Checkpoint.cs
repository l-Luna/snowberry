using Celeste;

namespace Snowberry.Editor.Entities;

[Plugin("checkpoint")]
public class Plugin_Checkpoint : Entity {
    [Option("bg")] public string Background = "";

    public override void Render() {
        base.Render();

        int id = Editor.Instance.Map.Id.Key()?.ID ?? 0;
        string text = !string.IsNullOrWhiteSpace(Background) ? "objects/checkpoint/bg/" + Background : "objects/checkpoint/bg/" + id;
        if (GFX.Game.Has(text))
            GFX.Game[text].DrawJustified(Position, new(0.5f, 1));
    }
}