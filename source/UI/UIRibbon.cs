using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI; 

public class UIRibbon : UIElement {
    private Font font;
    public string Text { get; private set; }

    public Color FG = Util.Colors.White;
    public Color BG = Calc.HexToColor("f25c54");
    public Color BGAccent = Calc.HexToColor("f7b267");

    private readonly int leftSpace, rightSpace;
    private readonly bool leftEdge, rightEdge;
    public int Accent = 1;

    public UIRibbon(string text, int leftSpace = 8, int rightSpace = 8, bool leftEdge = false, bool rightEdge = true)
        : this(text, Fonts.Regular, leftSpace, rightSpace, leftEdge, rightEdge) {
    }

    public UIRibbon(string text, Font font, int leftSpace = 8, int rightSpace = 8, bool leftEdge = false, bool rightEdge = true) {
        Text = text;
        this.font = font;
        this.leftSpace = leftSpace;
        this.rightSpace = rightSpace;
        this.leftEdge = leftEdge;
        this.rightEdge = rightEdge;

        Vector2 size = font.Measure(text);
        Width = (int)size.X + leftSpace + rightSpace + (leftEdge ? 5 : 0) + (rightEdge ? 5 : 0);
        Height = (int)size.Y;
    }

    public void SetText(string text, Font font = null) {
        this.font = font ?? this.font;
        Vector2 size = this.font.Measure(Text = text);
        Width = (int)size.X + leftSpace + rightSpace + (leftEdge ? 5 : 0) + (rightEdge ? 5 : 0);
        Height = (int)size.Y;
    }

    public override void Render(Vector2 position = default) {
        base.Render(position);

        int length = Width - (leftEdge ? 5 : 0) - (rightEdge ? 5 : 0);
        int offset = leftEdge ? 5 : 0;
        if (Accent > 0) {
            if (rightEdge)
                Draw.Rect(position + Vector2.UnitX * (length + offset), Accent, Height, BGAccent);
            if (leftEdge)
                Draw.Rect(position + Vector2.UnitX * (offset - Accent), Accent, Height, BGAccent);
        }

        Draw.Rect(position + Vector2.UnitX * offset, length, Height, BG);
        if (leftEdge) {
            if (Accent > 0)
                Fonts.Regular.Draw("\uE0B2", position - Vector2.UnitX * Accent, Vector2.One, BGAccent);
            Fonts.Regular.Draw("\uE0B2", position, Vector2.One, BG);
        }

        if (rightEdge) {
            if (Accent > 0)
                Fonts.Regular.Draw("\uE0B0", new Vector2(position.X + length + offset + Accent, position.Y), Vector2.One, BGAccent);
            Fonts.Regular.Draw("\uE0B0", new Vector2(position.X + length + offset, position.Y), Vector2.One, BG);
        }

        font.Draw(Text, new Vector2(position.X + offset + leftSpace, position.Y), Vector2.One, FG);
    }
}