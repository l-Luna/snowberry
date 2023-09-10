using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.IO;

namespace Snowberry.Editor;

public class Decal {
    private MTexture texture;
    public Vector2 Position;
    public Vector2 Scale;
    public Room Room;
    public float Rotation = 0;
    public Color Color = Color.White;

    public string Texture { get; private set; }

    public Rectangle Bounds => new((int)(Position.X - Math.Abs(texture.Width * Scale.X) / 2 + Room.X * 8), (int)(Position.Y - Math.Abs(texture.Height * Scale.Y) / 2 + Room.Y * 8), (int)Math.Abs(texture.Width * Scale.X), (int)Math.Abs(texture.Height * Scale.Y));

    internal Decal(Room room, string texture) {
        Room = room;
        this.texture = GFX.Game[texture];
        //this.Texture = texture;
    }

    internal Decal(Room room, DecalData data) {
        Room = room;

        // messy, see Celeste.Decal.orig_ctor
        var ext = Path.GetExtension(data.Texture);
        texture = GFX.Game[Path.Combine("decals", Texture = ext.Length > 0 ? data.Texture.Replace(Path.GetExtension(data.Texture), "") : data.Texture).Replace('\\', '/')];
        Position = data.Position;
        Scale = data.Scale;
        Rotation = data.Rotation;
        Color = Calc.HexToColorWithAlpha(data.ColorHex);
    }

    internal void Render(Vector2 offset) {
        texture.DrawCentered(offset + Position, Color, Scale, Rotation);
    }
}