using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Celeste;
using JetBrains.Annotations;
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

    public static readonly List<Keys> DigitKeys = [
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0
    ];

    public static int Bit(this bool b) => b ? 1 : 0;

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
        if (t == typeof(Color))
            return Color.White;
        return null;
    }

    public static Rectangle Multiply(this Rectangle r, int factor) =>
        new(r.X * factor, r.Y * factor, r.Width * factor, r.Height * factor);

    public static Rectangle Intersect(this Rectangle r, Rectangle other) {
        int newLeft = Math.Max(r.Left, other.Left),
            newTop = Math.Max(r.Top, other.Top),
            newRight = Math.Min(r.Right, other.Right),
            newBottom = Math.Min(r.Bottom, other.Bottom);
        return new Rectangle(newLeft, newTop, newRight - newLeft, newBottom - newTop);
    }

    public static Point ToPoint(this Vector2 v) =>
        new((int)v.X, (int)v.Y);

    public static Rectangle ToRect(this Vector2 v) =>
        new((int)v.X, (int)v.Y, 0, 0);

    public static Vector2 RoundTo(this Vector2 v, float interval) =>
        (v / interval).Round() * interval;

    public static string Substitute(this string s, params object[] values) {
        for (int i = 0; i < values.Length; i++)
            s = s.Replace($"{{{i}}}", values[i].ToString());
        return s;
    }

    public static string ToHex(this byte b) => BitConverter.ToString([b]).ToLower();

    public static string IntoRgbString(this Color c) =>
        $"{c.R.ToHex()}{c.G.ToHex()}{c.B.ToHex()}".ToLower();

    public static IEnumerable<T> ConcatN<T>(this IEnumerable<T> start, params T[] next) =>
        start.Concat(next); // the actual work is done by the compiler's interpretation of `params`

    public static string IntoRgbaString(this Color c) {
        float f = 255f / c.A;
        return c.A == 0 ? "00000000" : BitConverter.ToString(
            [(byte)Math.Round(c.R * f), (byte)Math.Round(c.G * f), (byte)Math.Round(c.B * f), c.A]
        ).Replace("-", string.Empty);
    }

    public static string ToInvString(this object o) => o is IConvertible c ? c.ToString(CultureInfo.InvariantCulture) : o.ToString();

    public static Dictionary<K, V> OrElse<K, V>(this Dictionary<K, V> primary, Dictionary<K, V> fallback) {
        var result = new Dictionary<K, V>(primary);
        foreach(KeyValuePair<K,V> p in fallback)
            result.TryAdd(p.Key, p.Value);
        return result;
    }

    // adapted from https://stackoverflow.com/a/39857727
    // count the number of times a horizontal line from the point intersects the polygon; odd -> inside
    public static bool PointInPolygon(Vector2 point, List<Vector2> polygon) {
        if (polygon.Count < 3)
            return false;
        bool isInPolygon = false;
        var lastVertex = polygon[^1];
        foreach (var vertex in polygon) {
            if (/*point.Y.IsBetween(lastVertex.Y, vertex.Y)*/ (point.Y - lastVertex.Y) * (point.Y - vertex.Y) < 0) {
                double t = (point.Y - lastVertex.Y) / (vertex.Y - lastVertex.Y);
                double x = t * (vertex.X - lastVertex.X) + lastVertex.X;
                if (x >= point.X)
                    isInPolygon = !isInPolygon;
            } else { // in case the point was exactly equal to a part of the polygon
                if (point.Y == lastVertex.Y && point.X < lastVertex.X && vertex.Y > point.Y)
                    isInPolygon = !isInPolygon;
                if (point.Y == vertex.Y && point.X < vertex.X && lastVertex.Y > point.Y)
                    isInPolygon = !isInPolygon;
            }

            lastVertex = vertex;
        }

        return isInPolygon;
    }

    public static K LookupName<K, V>(this Dictionary<K, V> dict, V value, K fallback) {
        // clown language
        var pairs = dict.Where(pair => pair.Value.ToString().Equals(value.ToString()));
        return pairs.Any() ? pairs.FirstOrDefault().Key : fallback;
    }

    [CanBeNull]
    public static BinaryPacker.Element Find(this BinaryPacker.Element e, string name) =>
        e.Children.Find(x => x.Name == name);

    public static BinaryPacker.Element FindOrEmpty(this BinaryPacker.Element e, string name) =>
        e.Find(name) ?? new BinaryPacker.Element { Children = [] };

    public static float Mod(float x, float m) => (x % m + m) % m;
    public static float Wrap(float val, float min, float max) => Mod(val - min, max - min) + min;
}