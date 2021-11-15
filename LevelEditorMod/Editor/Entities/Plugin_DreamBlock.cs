﻿using Microsoft.Xna.Framework;
using Monocle;

namespace LevelEditorMod.Editor.Entities {
    [Plugin("dreamBlock")]
    public class Plugin_DreamBlock : Entity {
        [Option("fastMoving")] public bool Fast = false;
        [Option("oneUse")] public bool OneUse = false;
        [Option("below")] public bool Below = false;

        public override int MinWidth => 8;
        public override int MinHeight => 8;
        public override int MaxNodes => 1;

        public override void Render() {
            base.Render();

            Draw.Rect(Position, Width, Height, Color.Black * 0.25f);
            Draw.HollowRect(Position, Width, Height, Color.White);
            if (Nodes.Length != 0)
                DrawUtil.DottedLine(Center, Nodes[0] + new Vector2(Width, Height) / 2f, Color.White, 4, 2);
        }

        protected override Rectangle[] Select() {
            if (Nodes.Length != 0) {
                Vector2 node = Nodes[0];
                return new Rectangle[] {
                    Bounds, new Rectangle((int)node.X, (int)node.Y, Width, Height)
                };
            } else {
                return new Rectangle[] { Bounds };
            }
        }

        public static void AddPlacements() {
            Placements.Create("Dream Block", "dreamBlock");
        }
    }
}
