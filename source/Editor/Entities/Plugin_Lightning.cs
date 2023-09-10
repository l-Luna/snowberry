using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Snowberry.Editor.Entities;

[Plugin("lightning")]
public class Plugin_Lightning : Entity {
    [Option("moveTime")] public float MoveTime = 1;

    private List<bool> upperEdges;
    private List<bool> lowerEdges;
    private List<bool> leftEdges;
    private List<bool> rightEdges;

    public static Color[] ElectricityColors = {
        Calc.HexToColor("8cf7e2"),
        Calc.HexToColor("fcf579")
    };

    public override int MinWidth => 8;
    public override int MinHeight => 8;
    public override int MaxNodes => 1;

    public Plugin_Lightning() {
        Tracked = true;
    }

    public override void Render() {
        base.Render();

        Draw.Rect(Position, Width, Height, ElectricityColors[0] * 0.15f);

        if (Editor.FancyRender) {
            const int slice = 4;
            int horizLimit = Width / (slice * 2);
            int vertLimit = Height / (slice * 2);

            bool dirty = upperEdges == null || Room == null || Room.IsEntityTypeDirty(typeof(Plugin_Lightning));
            if (dirty) {
                upperEdges = new List<bool>(horizLimit);
                lowerEdges = new List<bool>(horizLimit);
                leftEdges = new List<bool>(vertLimit);
                rightEdges = new List<bool>(vertLimit);
            }

            int prev0 = 0, prev1 = 0, prev3 = 0, prev4 = 0;
            for (int j = 0; j < horizLimit; j++) {
                if (upperEdges.Count <= j) {
                    upperEdges.Add(false);
                    lowerEdges.Add(false);
                }

                bool drawUp = dirty ? upperEdges[j] = !IsLightningAt(Position.X + j * slice * 2 + 2, Position.Y - 4) : upperEdges[j];
                bool drawDown = dirty ? lowerEdges[j] = !IsLightningAt(Position.X + j * slice * 2 + 2, Position.Y + 4 + Height) : lowerEdges[j];
                for (int k = 0; k < 2; k++) {
                    int i = j * 2 + k;
                    bool last = i + 1 >= Width / slice;
                    if (drawUp) {
                        Draw.Line(Position + new Vector2(i * slice, prev0), Position + new Vector2(i * slice + slice, prev0 = (last ? 0 : Calc.Random.Next(-2, 3))), ElectricityColors[0]);
                        Draw.Line(Position + new Vector2(i * slice, prev1), Position + new Vector2(i * slice + slice, prev1 = (last ? 0 : Calc.Random.Next(-2, 3))), ElectricityColors[1]);
                    }

                    if (drawDown) {
                        Draw.Line(Position + new Vector2(i * slice, Height + prev3), Position + new Vector2(i * slice + slice, Height + (prev3 = (last ? 0 : Calc.Random.Next(-2, 3)))), ElectricityColors[0]);
                        Draw.Line(Position + new Vector2(i * slice, Height + prev4), Position + new Vector2(i * slice + slice, Height + (prev4 = (last ? 0 : Calc.Random.Next(-2, 3)))), ElectricityColors[1]);
                    }
                }
            }

            prev0 = 0;
            prev1 = 0;
            prev3 = 0;
            prev4 = 0;
            for (int j = 0; j < vertLimit; j++) {
                if (leftEdges.Count <= j) {
                    leftEdges.Add(false);
                    rightEdges.Add(false);
                }

                bool drawLeft = dirty ? leftEdges[j] = !IsLightningAt(Position.X - 4, Position.Y + j * slice * 2 + 2) : leftEdges[j];
                bool drawRight = dirty ? rightEdges[j] = !IsLightningAt(Position.X + 4 + Width, Position.Y + j * slice * 2 + 2) : rightEdges[j];
                for (int k = 0; k < 2; k++) {
                    int i = j * 2 + k;
                    bool last = i + 1 >= Height / slice;
                    if (drawLeft) {
                        Draw.Line(Position + new Vector2(prev0, i * slice), Position + new Vector2(prev0 = (last ? 0 : Calc.Random.Next(-2, 3)), i * slice + slice), ElectricityColors[0]);
                        Draw.Line(Position + new Vector2(prev1, i * slice), Position + new Vector2(prev1 = (last ? 0 : Calc.Random.Next(-2, 3)), i * slice + slice), ElectricityColors[1]);
                    }

                    if (drawRight) {
                        Draw.Line(Position + new Vector2(Width + prev3, i * slice), Position + new Vector2(Width + (prev3 = (last ? 0 : Calc.Random.Next(-2, 3))), i * slice + slice), ElectricityColors[0]);
                        Draw.Line(Position + new Vector2(Width + prev4, i * slice), Position + new Vector2(Width + (prev4 = (last ? 0 : Calc.Random.Next(-2, 3))), i * slice + slice), ElectricityColors[1]);
                    }
                }
            }
        } else
            Draw.HollowRect(Position, Width, Height, ElectricityColors[1]);

        if (Nodes.Count != 0)
            DrawUtil.DottedLine(Center, Nodes[0] + new Vector2(Width, Height) / 2f, Color.White, 4, 2);
    }

    protected bool IsLightningAt(float x, float y) {
        /*if(Room == null)
            return false;

        for(int i = 0; i < Room.TrackedEntities[typeof(Plugin_Lightning)].Count; i++)
            if(Room.TrackedEntities[typeof(Plugin_Lightning)][i] is Plugin_Lightning e && e.Bounds.Contains((int)x, (int)y))
                return true;

        return false;*/

        // this is faster
        return Room != null && Room.TrackedEntities.ContainsKey(typeof(Plugin_Lightning)) && Room.TrackedEntities[typeof(Plugin_Lightning)].Exists(e => e is Plugin_Lightning && e.Bounds.Contains((int)x, (int)y));
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return Bounds;
        if (Nodes.Count != 0)
            yield return RectOnAbsolute(Size, position: Nodes[0]);
    }

    public static void AddPlacements() {
        Placements.Create("Lightning", "lightning");
    }
}