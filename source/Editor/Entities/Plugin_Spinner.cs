using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using static Celeste.TrackSpinner;

namespace Snowberry.Editor.Entities;

[Plugin("spinner")]
public class Plugin_Spinner : Entity {
    [Option("attachToSolid")] public bool Attached = false;
    [Option("color")] public CrystalColor SpinnerColor = CrystalColor.Blue;
    [Option("dust")] public bool Dust = false;

    private List<Entity> connectTo;

    public Plugin_Spinner() {
        Tracked = true;
    }

    public override void InitializeAfter() {
        base.InitializeAfter();
        // Handle vanilla stuff
        Dust |= IsVanillaDust();
        SpinnerColor = GetColorForVanillaMap() ?? SpinnerColor;
    }

    public override void RenderBefore() {
        base.RenderBefore();
        if (Editor.FancyRender && !Dust) {
            CrystalColor color = SpinnerColor;

            var colourString = color switch {
                CrystalColor.Blue => "blue",
                CrystalColor.Red => "red",
                CrystalColor.Purple => "purple",
                _ => "white"
            };

            Color c = Color.White;
            if (color == CrystalColor.Rainbow) {
                c = Calc.HsvToColor(0.4f + Calc.YoYo(Position.Length() % 280 / 280) * 0.4f, 0.4f, 0.9f);
            }

            UpdateConnections();
            MTexture bg = GFX.Game[$"danger/crystal/bg_{colourString}00"];
            foreach (var item in connectTo)
                bg.DrawCentered(Position + (item.Position - Position) / 2, c);
        }
    }

    public override void Render() {
        base.Render();

        CrystalColor color = SpinnerColor;

        if (Dust) {
            GFX.Game["danger/dustcreature/base00"].DrawCentered(Position);
            GFX.Game["danger/dustcreature/center00"].DrawCentered(Position);
        } else {
            var colourString = color switch {
                CrystalColor.Blue => "blue",
                CrystalColor.Red => "red",
                CrystalColor.Purple => "purple",
                _ => "white"
            };
            Color c = Color.White;
            if (color == CrystalColor.Rainbow)
                c = Calc.HsvToColor(0.4f + Calc.YoYo(Position.Length() % 280 / 280) * 0.4f, 0.4f, 0.9f);
            MTexture spinner = GFX.Game[$"danger/crystal/fg_{colourString}03"];
            spinner.DrawCentered(Position, c);
        }
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(14), justify: new(0.5f));
    }

    private void UpdateConnections() {
        if (connectTo == null || Room.IsEntityTypeDirty(typeof(Plugin_Spinner))) {
            connectTo = [];
            foreach (var item in Room.TrackedEntities[typeof(Plugin_Spinner)]) {
                if ((item.Position - Position).LengthSquared() < 24 * 24) {
                    connectTo.Add(item);
                }
            }
        }
    }

    public static void AddPlacements() {
        string[] types = ["Blue", "Red", "Purple", "Rainbow"];
        foreach (var type in types)
            Placements.EntityPlacementProvider.Create($"Spinner ({type})", "spinner", new() { ["color"] = type });

        Placements.EntityPlacementProvider.Create("Dust Sprite", "spinner", new() { ["dust"] = true });
    }

    public static CrystalColor? GetColorForVanillaMap() {
        return Editor.Instance.Map.Id.Key()?.ID switch {
            5 => CrystalColor.Red,
            6 => CrystalColor.Purple,
            10 => CrystalColor.Rainbow,
            _ => null
        };
    }

    public bool IsVanillaDust() {
        int? id = Editor.Instance.Map.Id.Key()?.ID;
        return id == 3 || (id == 7 && (Room?.Name?.StartsWith("d-") ?? false));
    }
}

public class Plugin_MovingSpinner : Entity {
    [Option("dust")] public bool Dust = false;
    [Option("star")] public bool Star = false;

    public override int MinNodes => 1;
    public override int MaxNodes => 1;

    public override void InitializeAfter() {
        base.InitializeAfter();
        Dust |= IsVanillaDust();
        Star |= IsVanillaStar();
    }

    public override void Render() {
        base.Render();

        Vector2 stop = Nodes[0];

        if (Star) {
            MTexture star = GFX.Game["danger/starfish13"];
            star.DrawCentered(Position);
            star.DrawCentered(stop);
        } else if (Dust) {
            MTexture dustbase = GFX.Game["danger/dustcreature/base00"],
                dustcenter = GFX.Game["danger/dustcreature/center00"];
            dustbase.DrawCentered(Position);
            dustcenter.DrawCentered(Position);
            dustbase.DrawCentered(stop);
            dustcenter.DrawCentered(stop);
        } else {
            MTexture blade = GFX.Game["danger/blade00"];
            blade.DrawCentered(Position);
            blade.DrawCentered(stop);
        }
    }

    protected override IEnumerable<Rectangle> Select() {
        yield return RectOnRelative(new(14), justify: new(0.5f));
        yield return RectOnAbsolute(new(14), Nodes[0], justify: new(0.5f));
    }

    public bool IsVanillaDust() {
        int? id = Editor.Instance.Map.Id.Key()?.ID;
        return id == 3 || (id == 7 && ((Room ?? Editor.SelectedRoom)?.Name?.StartsWith("d-") ?? false));
    }

    public bool IsVanillaStar() {
        return Editor.Instance.Map.Id.Key()?.ID == 10;
    }
}

[Plugin("trackSpinner")]
public class Plugin_TrackSpinner : Plugin_MovingSpinner {
    [Option("speed")] public Speeds Speed = Speeds.Normal;
    [Option("startCenter")] public bool StartAtCenter = false;

    public override void Render() {
        DrawUtil.DottedLine(Position, Nodes[0], Color.White * 0.5f, 8, 4);
        base.Render();
    }

    public static void AddPlacements() {
        Placements.EntityPlacementProvider.Create("Moving Blade", "trackSpinner");
        Placements.EntityPlacementProvider.Create("Moving Dust", "trackSpinner", new() { ["dust"] = true });
        Placements.EntityPlacementProvider.Create("Moving Star", "trackSpinner", new() { ["star"] = true });
    }
}

[Plugin("rotateSpinner")]
public class Plugin_RotateSpinner : Plugin_MovingSpinner {
    [Option("clockwise")] public bool Clockwise = false;

    public override void Render() {
        Draw.Circle(Position, Vector2.Distance(Position, Nodes[0]), Color.White * 0.5f, 20);
        base.Render();
    }

    public static void AddPlacements() {
        Placements.EntityPlacementProvider.Create("Rotating Blade", "rotateSpinner");
        Placements.EntityPlacementProvider.Create("Rotating Dust", "rotateSpinner", new() { ["dust"] = true });
        Placements.EntityPlacementProvider.Create("Rotating Star", "rotateSpinner", new() { ["star"] = true });
    }
}