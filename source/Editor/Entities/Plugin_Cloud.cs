using Celeste;
using System.Collections.Generic;

namespace Snowberry.Editor.Entities;

[Plugin("cloud")]
public class Plugin_Cloud : Entity {
    [Option("fragile")] public bool Fragile = false;

    public override void Render() {
        base.Render();

        string type = Fragile ? "cloudFragile" : "cloud";
        string suffix = (Editor.Instance.Map.Id.Key()?.Mode) == AreaMode.Normal ? "" : "Remix";
        FromSprite(type + suffix, "idle")?.DrawCentered(Position);
    }

    public static void AddPlacements() {
        Placements.EntityPlacementProvider.Create("Cloud", "cloud");
        Placements.EntityPlacementProvider.Create("Cloud (Fragile)", "cloud", new Dictionary<string, object>() { { "fragile", true } });
    }
}