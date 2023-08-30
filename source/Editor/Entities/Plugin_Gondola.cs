using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

[Plugin("gondola")]
public class Plugin_Gondola : Entity {

    // jank jank broken jank please help me
    // https://discord.com/channels/920506235927793675/1144069604491149364/1146216330882728126

    public static readonly MTexture[] sprites = {
        GFX.Game["objects/gondola/back"],
        GFX.Game["objects/gondola/front"],
        GFX.Game["objects/gondola/lever00"],
        GFX.Game["objects/gondola/top"],
        GFX.Game["objects/gondola/cliffsideLeft"],
        GFX.Game["objects/gondola/cliffsideRight"]
    };

    [Option("active")] public bool Active = false;

    public override int MinNodes => 1;
    public override int MaxNodes => 1;
    public bool activePrevState;
    public Vector2 leftPartPos;
    public Vector2 rightPartPos;

    public override void Initialize() {
        activePrevState = Active;

        switch (Editor.VanillaLevelID) {
            case 4:
                leftPartPos = Position + new Vector2(-96, -16);
                rightPartPos = (Vector2)GetNode(0) + new Vector2(120, -104);
            break;
            case 5:
                leftPartPos = (Vector2)GetNode(0);
                rightPartPos = Position + new Vector2(120, -104);
                break;
            default:
                leftPartPos = Position + new Vector2(-96, -16);
                rightPartPos = Position + new Vector2(120, -104);
                break;
        }

        SetNode(0, (Active ? rightPartPos : leftPartPos));
    }

    public override void Render() {
        if (activePrevState != Active) {
            Vector2 pos = (Active) ? rightPartPos : leftPartPos;
            SetNode(0, pos);
            activePrevState = Active;
        }

        if (Active) {
            rightPartPos = Nodes[0];
        } else {
            leftPartPos = Nodes[0];
        }

        base.Render();
        Vector2 posBullshit;
        switch (Editor.VanillaLevelID) {
            case 5:
                posBullshit = Nodes[0];
                break;
            default:
                posBullshit = Position;
                break;
        }

        Draw.Line(leftPartPos + new Vector2(6, 10), posBullshit + new Vector2(0, -54), Color.Black, 1);
        Draw.Line(posBullshit + new Vector2(0, -54), rightPartPos + new Vector2(-10, -4), Color.Black, 1);
        sprites[4].DrawCentered(leftPartPos);
        sprites[5].DrawCentered(rightPartPos, Color.White, new Vector2(-1, 1));

        sprites[0].DrawJustified(posBullshit, new(0.5f, 0.8f));
        sprites[1].DrawJustified(posBullshit, new(0.5f, 0.8f));
        if (Active)
            sprites[2].DrawJustified(posBullshit, new(0.5f, 0.8f));
        // TODO: Visually update top with rotation based on angle between left and right parts
        sprites[3].DrawJustified(posBullshit, new(0.5f, 0.8f));
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(74, 73), justify: new(0.5f, 0.8f));


        Vector2 boxSize = Active ? new(44, 26) : new(44, 16);
        Vector2 posOffset = Active ? new(2, -3) : new(-3, 0);
        Vector2 justifyAmt = Active ? new(0.5f) : new(0.5f, 0);
        yield return RectOnAbsolute(boxSize, position: Nodes[0] + posOffset, justify: justifyAmt);
    }

    public static void AddPlacements() {
        Placements.Create("Gondola", "gondola");
    }
}