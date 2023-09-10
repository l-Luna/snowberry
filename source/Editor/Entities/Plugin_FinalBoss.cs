using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("finalBoss")]
public class Plugin_FinalBoss : Entity {
    [Option("patternIndex")] public int PatternIndex = 1;
    [Option("startHit")] public bool StartHit = false;
    [Option("cameraPastY")] public float cameraPastY = 120.0f;
    [Option("cameraLockY")] public bool CameraLockY = true;
    [Option("canChangeMusic")] public bool CanChangeMusic = true;

    public override int MaxNodes => -1;

    public override void Render() {
        base.Render();

        MTexture baddy = FromSprite("badeline_boss", "attack2Begin");
        baddy?.DrawCentered(Position);

        foreach (Vector2 node in Nodes)
            baddy?.DrawCentered(node);
    }

    public override void HQRender() {
        base.HQRender();

        Vector2 prev = Position;
        foreach (Vector2 node in Nodes) {
            DrawUtil.DottedLine(prev, node, Color.Red * 0.5f, 8, 4);
            prev = node;
        }
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(38, 30), position: new(0, 2), justify: new(0.5f));
        foreach (var node in Nodes)
            yield return RectOnAbsolute(new(38, 30), position: node + new Vector2(0, 2), justify: new(0.5f));
    }

    public static void AddPlacements() {
        Placements.Create("Badeline Boss", "finalBoss");
    }
}