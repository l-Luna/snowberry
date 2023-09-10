using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI;

class UILabel : UIElement {
    private readonly Font font;

    public Func<string> Value { get; private set; }
    public Color FG = Calc.HexToColor("f0f0f0");
    public bool Underline = false, Strikethrough = false;
    public string LabelTooltip;
    public float Scale = 1;

    public UILabel(Func<string> text) : this(Fonts.Regular, (int)Fonts.Regular.Measure(text()).X, text, 1) { }

    public UILabel(string text) : this(Fonts.Regular, (int)Fonts.Regular.Measure(text).X, () => text, 1) { }

    public UILabel(string text, Font font) : this(font, (int)font.Measure(text).X, () => text, 1) { }

    public UILabel(string text, float scale) : this(Fonts.Regular, (int)(Fonts.Regular.Measure(text).X * scale), () => text, scale) { }

    public UILabel(string text, Font font, float scale) : this(font, (int)(font.Measure(text).X * scale), () => text, scale) { }

    public UILabel(Font font, int width, Func<string> input, float scale) {
        this.font = font;
        Value = input;
        Scale = scale;
        Width = Math.Max(1, width);
        Height = (int)(font.LineHeight * Scale);
    }

    public override void Render(Vector2 position = default) {
        base.Render(position);

        font.Draw(Value(), position, new(Scale), FG);
        if (Underline)
            Draw.Rect(position + Vector2.UnitY * Height, Width, Scale, FG);
        if (Strikethrough)
            Draw.Rect(position + Vector2.UnitY * (Height / 2), Width, Scale, Color.Lerp(FG, Color.Black, 0.25f));
    }

    public override string Tooltip() => LabelTooltip;
}