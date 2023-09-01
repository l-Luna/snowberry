using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor.Entities;

public abstract class Plugin_TileEntityBase : Entity {
    protected virtual float Alpha => 1.0f;
    protected VirtualMap<MTexture> Tiles;

    public override int MinWidth => 8;
    public override int MinHeight => 8;

    public override void Render() {
        base.Render();

        if (Tiles != null) {
            Color c = Color.White * Alpha;
            for (int x = 0; x < Tiles.Columns; x++)
                for (int y = 0; y < Tiles.Rows; y++)
                    Tiles[x, y]?.Draw(Position + new Vector2(x, y) * 8, Vector2.Zero, c);
        }
    }
}

public abstract class Plugin_TileEntity : Plugin_TileEntityBase {
    [Option("tiletype")] public Tileset TileType = Tileset.ByKey('3', false);

    private Tileset last;

    public override void Initialize() {
        base.Initialize();
        Tiles = GFX.FGAutotiler.GenerateBox(TileType.Key, Width / 8, Height / 8).TileGrid.Tiles;
        last = TileType;
    }

    public override void Render() {
        if (last != TileType || Dirty) {
            Tiles = GFX.FGAutotiler.GenerateBox(TileType.Key, Width / 8, Height / 8).TileGrid.Tiles;
            last = TileType;
        }

        base.Render();
    }
}

[Plugin("introCrusher")]
public class Plugin_IntroCrusher : Plugin_TileEntity {
    [Option("flags")] public string Flags = "1,0b";

    public override int MinNodes => 1;
    public override int MaxNodes => 1;

    public override void Render() {
        base.Render();

        if (Tiles != null && Nodes.Count > 0) {
            Color c = Color.White * 0.25f;
            for (int x = 0; x < Tiles.Columns; x++)
                for (int y = 0; y < Tiles.Rows; y++)
                    Tiles[x, y]?.Draw(Nodes[0] + new Vector2(x, y) * 8, Vector2.Zero, c);
        }

        DrawUtil.DottedLine(Center, Nodes[0] + new Vector2(Width, Height) / 2, Color.White, 8, 4);
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(Width, Height));
        yield return RectOnAbsolute(new(Width, Height), position: Nodes[0]);
    }

    public static void AddPlacements() {
        Placements.Create("Intro Crusher", "introCrusher");
    }
}

[Plugin("finalBossMovingBlock")]
public class Plugin_BadelineBossMovingBlock : Plugin_TileEntityBase {
    [Option("nodeIndex")] public int NodeIndex = 0;

    public override int MinNodes => 1;
    public override int MaxNodes => 1;

    public override void Render() {
        if (Tiles == null || Dirty)
            Tiles = GFX.FGAutotiler.GenerateBox('g', Width / 8, Height / 8).TileGrid.Tiles;

        if (Tiles != null && Nodes.Count > 0) {
            for (int x = 0; x < Tiles.Columns; x++)
                for (int y = 0; y < Tiles.Rows; y++) {
                    Tiles[x, y]?.Draw(Position + new Vector2(x, y) * 8, Vector2.Zero, Color.White);
                    Tiles[x, y]?.Draw(Nodes[0] + new Vector2(x, y) * 8, Vector2.Zero, Color.White * 0.15f);
                }
        }

        DrawUtil.DottedLine(Center, Nodes[0] + new Vector2(Width, Height) / 2, Color.White, 8, 4);
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(Width, Height));
        yield return RectOnAbsolute(new(Width, Height), position: Nodes[0]);
    }

    public static void AddPlacements() {
        Placements.Create("Boss Moving Block", "finalBossMovingBlock");
    }
}

[Plugin("finalBossFallingBlock")]
public class Plugin_BadelineBossFallingBlock : Plugin_TileEntityBase {

    public override void Render() {
        if (Tiles == null || Dirty)
            Tiles = GFX.FGAutotiler.GenerateBox('g', Width / 8, Height / 8).TileGrid.Tiles;

        base.Render();
    }

    public static void AddPlacements() {
        Placements.Create("Boss Falling Block", "finalBossFallingBlock");
    }
}

[Plugin("coverupWall")]
[Plugin("fakeWall")]
public class Plugin_FakeWall : Plugin_TileEntity {
    protected override float Alpha => 0.7f;

    public static void AddPlacements() {
        Placements.Create("Fake Wall", "fakeWall");
        Placements.Create("Coverup Wall", "coverupWall");
    }
}

[Plugin("fakeBlock")]
//[Plugin("exitBlock")]
public class Plugin_FakeBlock : Plugin_FakeWall {
    [Option("playTransitionReveal")] public bool PlayTransitionReveal = false;

    public new static void AddPlacements() {
        Placements.Create("Fake Block", "fakeBlock");
    }
}

[Plugin("exitBlock")]
// would have made it just another FakeBlock, but it uses `tileType` instead???
// TODO: allow renaming options in subtypes/redirecting properties instead of this BS
public class Plugin_ExitBlock : Plugin_TileEntityBase {
    protected override float Alpha => 0.7f;

    [Option("tileType")] public Tileset TileType = Tileset.ByKey('3', false);
    [Option("playTransitionReveal")] public bool PlayTransitionReveal = false;

    private Tileset last;

    public override void Initialize() {
        base.Initialize();
        Tiles = GFX.FGAutotiler.GenerateBox(TileType.Key, Width / 8, Height / 8).TileGrid.Tiles;
        last = TileType;
    }

    public override void Render() {
        if (last != TileType || Dirty) {
            Tiles = GFX.FGAutotiler.GenerateBox(TileType.Key, Width / 8, Height / 8).TileGrid.Tiles;
            last = TileType;
        }

        base.Render();
    }

    public static void AddPlacements() {
        Placements.Create("Exit Block", "exitBlock");
    }
}

[Plugin("conditionBlock")]
public class Plugin_ConditionBlock : Plugin_FakeWall {
    [Option("condition")] public string Condition = "Key";
    [Option("conditionID")] public string ConditionID = "1:1";

    public new static void AddPlacements() {
        Placements.Create("Condition Block", "conditionBlock");
    }
}

[Plugin("floatySpaceBlock")]
public class Plugin_FloatySpaceBlock : Plugin_TileEntity {
    [Option("disableSpawnOffset")] public bool DisableOffset = false;

    public static void AddPlacements() {
        Placements.Create("Floaty Space Block", "floatySpaceBlock");
    }
}

[Plugin("crumbleWallOnRumble")]
public class Plugin_CrumbleWallOnRumble : Plugin_TileEntity {
    [Option("blendin")] public bool BlendIn = true;
    [Option("persistent")] public bool Persistent = false;

    public override void ChangeDefault() {
        base.ChangeDefault();
        TileType = Tileset.ByKey('m', false);
    }

    public static void AddPlacements() {
        Placements.Create("Crumble Wall on Rumble", "crumbleWallOnRumble");
    }
}

[Plugin("dashBlock")]
public class Plugin_DashBlock : Plugin_TileEntity {
    [Option("blendin")] public bool BlendIn = true;
    [Option("canDash")] public bool CanDash = true;
    [Option("permanent")] public bool Permanent = false;

    public static void AddPlacements() {
        Placements.Create("Dash Block", "dashBlock");
    }
}

[Plugin("fallingBlock")]
public class Plugin_FallingBlock : Plugin_TileEntity {
    [Option("climbFall")] public bool ClimbFall = true;
    [Option("behind")] public bool Behind = false;

    public static void AddPlacements() {
        Placements.Create("Falling Block", "fallingBlock");
    }
}