using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Snowberry;

public static class Util {
    public static class Colors {
        public static readonly Color White = Calc.HexToColor("f0f0f0");
        public static readonly Color Cyan = Calc.HexToColor("1fc4bc");
        public static readonly Color DarkCyan = Calc.HexToColor("1da4ad");
        public static readonly Color Red = Calc.HexToColor("db2323");
        public static readonly Color DarkRed = Calc.HexToColor("b02020");
        public static readonly Color Blue = Calc.HexToColor("2877de");
        public static readonly Color DarkBlue = Calc.HexToColor("2365cf");
        public static readonly Color DarkGray = Calc.HexToColor("060607");
        public static readonly Color CloudGray = Calc.HexToColor("293036");
        public static readonly Color CloudLightGray = Calc.HexToColor("5c646b");
    }

    public static readonly List<Keys> DigitKeys = new() {
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0
    };

    public static int Bit(this bool b) {
        return b ? 1 : 0;
    }

    public static object Default(Type t) {
        if (t.IsEnum)
            return t.GetEnumValues().GetValue(0);
        if (t == typeof(string))
            return "";
        if (t == typeof(int))
            return 0;
        if (t == typeof(short))
            return (short)0;
        if (t == typeof(byte))
            return (byte)0;
        if (t == typeof(long))
            return (long)0;
        if (t == typeof(float))
            return (float)0;
        if (t == typeof(double))
            return (double)0;
        if (t == typeof(bool))
            return false;
        return null;
    }

    // from FileProxy
    public static string Modize(string path) {
        string directoryName = Path.GetDirectoryName(path);
        path = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrEmpty(directoryName))
            path = Path.Combine(directoryName, path);
        if (path.StartsWith(Everest.Content.PathContentOrig))
            path = path.Substring(Everest.Content.PathContentOrig.Length + 1);
        path = path.Replace('\\', '/');
        return path;
    }

    public static string GetRealPath(string path) {
        Everest.Content.TryGet(Modize(path), out ModAsset asset);
        return asset switch {
            FileSystemModAsset fs => fs.Path,
            MapBinsInModsModAsset map => map.Path,
            _ => null
        };
    }

    public static string KeyToPath(Celeste.AreaKey key) =>
        GetRealPath(Path.Combine("Maps", Celeste.AreaData.Get(key).Mode[(int)key.Mode].Path + ".bin"));

    public static Rectangle Multiply(this Rectangle r, int factor) =>
        new(r.X * factor, r.Y * factor, r.Width * factor, r.Height * factor);

    public static Point ToPoint(this Vector2 v) =>
        new((int)v.X, (int)v.Y);

    public static Rectangle ToRect(this Vector2 v) =>
        new((int)v.X, (int)v.Y, 0, 0);

    public static Vector2 RoundTo(this Vector2 v, float interval) =>
        (v / interval).Round() * interval;

    public static IEnumerable<T> ConcatN<T>(IEnumerable<T> start, params T[] next) =>
        start.Concat(next); // the actual work is done by the compiler's interpretation of `params`
}