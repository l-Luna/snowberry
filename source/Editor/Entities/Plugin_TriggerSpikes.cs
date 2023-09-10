using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Spikes;

namespace Snowberry.Editor.Entities;

[Plugin("triggerSpikesUp")]
[Plugin("triggerSpikesDown")]
[Plugin("triggerSpikesLeft")]
[Plugin("triggerSpikesRight")]
public class Plugin_TriggerSpikes : Entity {

    public static readonly Color[] EdgeColors = {
        Calc.HexToColor("f25a10"),
        Calc.HexToColor("ff0000"),
        Calc.HexToColor("f21067")
    };

    private Directions dir;
    private bool initialized = false;

    public override int MinWidth => (!initialized || dir == Directions.Left || dir == Directions.Right) ? -1 : 8;
    public override int MinHeight => (!initialized || dir == Directions.Up || dir == Directions.Down) ? -1 : 8;

    public override void Initialize() {
        base.Initialize();

        dir = Name switch {
            "triggerSpikesRight" => Directions.Right,
            "triggerSpikesLeft" => Directions.Left,
            "triggerSpikesDown" => Directions.Down,
            _ => Directions.Up,
        };
        initialized = true;
    }

    public override void Render() {
        base.Render();

        MTexture dust1 = GFX.Game["danger/triggertentacle/wiggle_v06"], dust2 = GFX.Game["danger/triggertentacle/wiggle_v03"];

        switch (dir) {
            default:
            case Directions.Up:
                for (int x = 0; x < Width / 4; x++) {
                    var second = x % 2 == 0;
                    (second ? dust1 : dust2).DrawJustified(Position + new Vector2(x * 4 + second.Bit() - 1, 0), new Vector2(0, 1), Calc.Random.Choose(EdgeColors), 1, 0);
                }

                break;

            case Directions.Down:
                for (int x = 0; x < Width / 4; x++) {
                    var second = x % 2 == 0;
                    (second ? dust1 : dust2).Draw(Position + new Vector2((x + 1) * 4 - second.Bit(), 3), Vector2.Zero, Calc.Random.Choose(EdgeColors), 1, MathHelper.Pi);
                }

                break;

            case Directions.Left:
                for (int y = 0; y < Height / 4; y++) {
                    var second = y % 2 == 0;
                    (second ? dust1 : dust2).DrawJustified(Position + new Vector2(-3, y * 4 - second.Bit()), new Vector2(1, 0), Calc.Random.Choose(EdgeColors), 1, MathHelper.Pi + MathHelper.PiOver2);
                }

                break;

            case Directions.Right:
                for (int y = 0; y < Height / 4; y++) {
                    var second = y % 2 == 0;
                    (second ? dust1 : dust2).Draw(Position + new Vector2(3, y * 4 + second.Bit() - 1), Vector2.Zero, Calc.Random.Choose(EdgeColors), 1, MathHelper.PiOver2);
                }

                break;
        }
    }

    public static void AddPlacements() {
        foreach (var dir in new[]{ "Up", "Down", "Left", "Right" })
            Placements.Create($"Trigger Spikes ({dir}, Dust)", "triggerSpikes" + dir);
    }
}