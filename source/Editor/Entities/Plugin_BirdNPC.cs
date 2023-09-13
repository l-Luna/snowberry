using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System.Collections.Generic;

namespace Snowberry.Editor.Entities;

[Plugin("bird")]
public class Plugin_BirdNPC : Entity {

    // This list of x-scales lines up with the order of the Modes enum. I am so sorry. I'm usually better I swear. - Gamation
    private static readonly int[] facingScales = {-1, 1, 1, -1, -1, -1, -1, 1, -1, -1};

    [Option("mode")] public BirdNPC.Modes Mode = BirdNPC.Modes.Sleeping;
    [Option("onlyOnce")] public bool OnlyOnce = false;
    [Option("onlyIfPlayerLeft")] public bool OnlyIfPlayerLeft = false;

    public override int MaxNodes => -1;

    public override void Render() {
        base.Render();

        MTexture sprite;

        switch (Mode) {
            case BirdNPC.Modes.Sleeping:
                sprite = GFX.Game["characters/bird/crow48"];
                break;
            case BirdNPC.Modes.WaitForLightningOff:
                sprite = GFX.Game["characters/bird/Hover04"];
                break;
            default:
                sprite = GFX.Game["characters/bird/crow00"];
                break;
        }

        Vector2 justifyMod = (Mode == BirdNPC.Modes.WaitForLightningOff) ? new Vector2(0.5f, 1) : new Vector2(0.5f, 1);
        sprite.DrawJustified(Position, justifyMod, Color.White, new Vector2(facingScales[(int)Mode], 1));

        if (Nodes.Count > 0)
            foreach (var node in Nodes)
                sprite?.DrawCentered(node, Color.White * 0.5f, new Vector2(facingScales[(int)Mode], 1));
    }

    public override void HQRender() {
        base.HQRender();

        Vector2 prev = Position;
        if (Nodes.Count > 0) {
            foreach (Vector2 node in Nodes) {
                DrawUtil.DottedLine(prev, node, Color.White * 0.5f, 8, 4);
                prev = node;
            }
        }
    }

    protected override IEnumerable<Rectangle> Select() {

        Vector2 selBoxSize = (Mode == BirdNPC.Modes.WaitForLightningOff) ? new(18, 12) : new(16, 16);
        Vector2 posModifier = (Mode == BirdNPC.Modes.WaitForLightningOff) ? new(0, 0): new(0, 0);
        yield return RectOnRelative(selBoxSize, position: posModifier, justify: new(0.5f, 1));

        foreach (var node in Nodes)
            yield return RectOnAbsolute(selBoxSize, position: node + posModifier, justify: new(0.5f, 0.5f));
    }

    public static void AddPlacements() {
        Placements.Create("Bird NPC", "bird");
    }
}