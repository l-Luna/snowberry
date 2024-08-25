using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Entities;

[Plugin("blackGem")]
public class Plugin_HeartGem : Entity {
    [Option("fake")] public bool Fake = false;
    [Option("removeCameraTriggers")] public bool RemoveCameraTriggers = false;
    [Option("fakeHeartDialog")] public string FakeHeartDialog = "CH9_FAKE_HEART";
    [Option("keepGoingDialog")] public string KeepGoingDialog = "CH9_KEEP_GOING";

    public override void Render() {
        base.Render();

        string texture = Editor.Instance.Map.Id.Key()?.Mode switch {
            AreaMode.BSide => "heartgem1",
            AreaMode.CSide => "heartgem2",
            _ => "heartgem0"
        };
        FromSprite(Fake ? "heartgem3" : texture, "idle")?.DrawCentered(Position);
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(18), justify: new(0.5f));
    }

    public static void AddPlacements() {
        Placements.EntityPlacementProvider.Create("Crystal Heart", "blackGem");
    }
}