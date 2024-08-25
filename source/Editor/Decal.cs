using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Celeste.Mod;

namespace Snowberry.Editor;

public partial class Decal : Placeable {
    public Room Room { get; set; }
    public Vector2 Position { get; set; }

    private readonly MTexture tex;

    public Vector2 Scale = new(1);
    public float Rotation = 0;
    public Color Color = Color.White;
    public bool Fg = false;
    public int? Depth = null;

    public string Texture { get; private set; }

    public Rectangle Bounds {
        get {
            float w = Math.Abs(tex.Width * Scale.X);
            float h = Math.Abs(tex.Height * Scale.Y);
            if (Rotation != 0) {
                Vector2 tL = new Vector2(-w/2, -h/2).Rotate(Rotation.ToRad());
                Vector2 bL = new Vector2(-w/2, h/2).Rotate(Rotation.ToRad());
                Vector2 tR = new Vector2(w/2, -h/2).Rotate(Rotation.ToRad());
                Vector2 bR = new Vector2(w/2, h/2).Rotate(Rotation.ToRad());
                w = 2 * Math.Abs(Math.Min(Math.Min(tL.X, bL.X), Math.Min(tR.X, bR.X)));
                h = 2 * Math.Abs(Math.Min(Math.Min(tL.Y, bL.Y), Math.Min(tR.Y, bR.Y)));
            }

            return new((int)(Position.X - w / 2), (int)(Position.Y - h / 2), (int)w, (int)h);
        }
    }

    internal Decal(Room room, string texture, bool fg) {
        Room = room;
        Texture = texture;
        Fg = fg;
        tex = LookupTex(texture);
    }

    internal Decal(Room room, DecalData data, bool fg) {
        Room = room;
        Fg = fg;

        Texture = data.Texture;
        tex = LookupTex(Texture);
        Position = data.Position + Room.Position * 8;
        Scale = data.Scale;
        Rotation = data.Rotation;
        Color = Calc.HexToColorWithAlpha(data.ColorHex);
        Depth = data.Depth;
    }

    public static Decal FromElement(Room room, BinaryPacker.Element element, bool fg) {
        // from LevelData
        DecalData decalData = new DecalData {
            Position = new Vector2(Convert.ToSingle(element.Attributes["x"], CultureInfo.InvariantCulture),
                Convert.ToSingle(element.Attributes["y"], CultureInfo.InvariantCulture)),
            Scale = new Vector2(Convert.ToSingle(element.Attributes["scaleX"], CultureInfo.InvariantCulture),
                Convert.ToSingle(element.Attributes["scaleY"], CultureInfo.InvariantCulture)),
            Texture = (string)element.Attributes["texture"],
            Rotation = element.AttrFloat("rotation"),
            ColorHex = element.Attr("color")
        };
        if (element.HasAttr("depth"))
            decalData.Depth = element.AttrNullableInt("depth");

        return new Decal(room, decalData, fg);
    }

    public void Render() {
        tex.DrawCentered(Position, Color, Scale, Rotation.ToRad());
    }

    public void AddToRoom(Room room) {
        Room = room;
        UndoRedo.BeginAction("add decal", Room.SnapshotDecalInclusion(this));
        (Fg ? room.FgDecals : room.BgDecals).Add(this);
        UndoRedo.CompleteAction();
    }

    private static MTexture LookupTex(string tex) =>
        // grab first variant of decal
        GFX.Game.GetAtlasSubtextures(Sanitize(tex, false))[0];

    public static string Sanitize(string tex, bool hasPfix){
        // see Celeste.Decal.orig_ctor
        // remove any extention like .png
        var ext = Path.GetExtension(tex);
        var plainPath = ext.Length > 0 ? tex[..^ext.Length] : tex;
        // put it in decals/ if necessary
        var pfixPath = hasPfix ? plainPath : "decals/" + plainPath;
        // fix any backslashes
        var ctxPath = pfixPath.Replace("\\", "/");
        // remove any numeric suffix
        return StripDigits().Replace(ctxPath, "");
    }

    public UndoRedo.Snapshotter SnapshotPosition() => new PositionSnapshotter(this);

    private record PositionSnapshotter(Decal d) : UndoRedo.Snapshotter<Vector2> {
        public Vector2 Snapshot() => d.Position;
        public void Apply(Vector2 t) => d.Position = t;
    }

    [GeneratedRegex("\\d+$", RegexOptions.Compiled)]
    private static partial Regex StripDigits();
}