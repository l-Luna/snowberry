using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI;

public class UIColorPicker : UIElement {
    private readonly int wheelWidth;
    private int svWidth, svHeight;
    private int colorPreviewSize;

    private readonly UITextField hexTextField;
    private bool hueEdit, svEdit;

    public Action<Color> OnColorChange;
    public Color Value { get; private set; }
    private float h, s, v;

    public UIColorPicker(int svWidth, int svHeight, int wheelWidth, int colorPreviewSize, Color color = default) {
        this.wheelWidth = wheelWidth;
        this.svWidth = svWidth;
        this.svHeight = svHeight;
        this.colorPreviewSize = Math.Max(colorPreviewSize, Fonts.Regular.LineHeight);
        Width = svWidth + wheelWidth - 2;
        Height = svHeight + colorPreviewSize - 2;

        Add(hexTextField = new UITextField(Fonts.Regular, 36) {
            Position = new Vector2(Width / 2 - 18, svHeight - 1),
            Line = Color.Transparent,
            LineSelected = Color.Transparent,
            BG = Color.Transparent,
            BGSelected = Color.Transparent,
        });

        SetColor(color);
        HSV(color, out h, out s, out v);
        GrabsClick = true;
    }

    public void SetColor(Color c) {
        Value = c;
        hexTextField.UpdateInput($"#{Value.IntoString().ToLower()}");
    }

    public override void Update(Vector2 position = default) {
        base.Update(position);

        int mouseX = (int)Mouse.Screen.X;
        int mouseY = (int)Mouse.Screen.Y;
        Rectangle svRect = new Rectangle((int)position.X + 1, (int)position.Y + 1, svWidth - 2, svHeight - 2);
        Rectangle wheelRect = new Rectangle((int)position.X + svWidth + 1, (int)position.Y + 1, wheelWidth - 2, svHeight - 2);

        if (MInput.Mouse.CheckLeftButton) {
            if (svRect.Contains(mouseX, mouseY) && ConsumeLeftClick()) {
                svEdit = true;
            } else if (wheelRect.Contains(mouseX, mouseY) && ConsumeLeftClick()) {
                hueEdit = true;
            }


            if (svEdit || hueEdit) {
                if (svEdit) {
                    s = Calc.Clamp(mouseX - position.X, 0, svWidth) / svWidth;
                    v = 1 - Calc.Clamp(mouseY - position.Y, 0, svHeight) / svHeight;
                } else if (hueEdit) {
                    h = Calc.Clamp(mouseY - position.Y, 0, svHeight) / svHeight;
                }

                SetColor(Calc.HsvToColor(h, s, v));
                OnColorChange?.Invoke(Value);
            }
        } else if (MInput.Mouse.ReleasedLeftButton)
            svEdit = hueEdit = false;
    }

    public override void Render(Vector2 position = default) {
        Draw.Rect(position + Vector2.UnitX * svWidth, wheelWidth - 3, svHeight, Color.Black);
        Draw.Rect(position + Vector2.UnitX, svWidth - 2, svHeight, Color.Black);
        Draw.Rect(position + Vector2.UnitY, svWidth + wheelWidth - 2, svHeight - 2, Color.Black);
        Draw.Rect(position + Vector2.UnitY * svHeight, Width, colorPreviewSize - 3, Color.Black);
        Draw.Rect(position + new Vector2(1, svHeight - 1), Width - 2, colorPreviewSize - 1, Color.Black);

        float w = svWidth - 2;
        float h = svHeight - 2;
        for (int x = 1; x <= w; x++)
            for (int y = 1; y <= h; y++)
                Draw.Point(position + new Vector2(x, y), Calc.HsvToColor(this.h, x / w, 1 - y / h));

        int wheel = wheelWidth - 3;
        for (int i = 1; i <= h; i++)
            Draw.Rect(position + new Vector2(svWidth, i), wheel, 1, Calc.HsvToColor(i / h, 1, 1));

        int hueX = (int)position.X + svWidth;
        int hueY = (int)position.Y + (int)(this.h * (svHeight - 3)) + 1;
        Draw.Rect(hueX, hueY - 1, wheel, 1, Color.Black);
        Draw.Rect(hueX, hueY + 1, wheel, 1, Color.Black);

        Vector2 svPos = position + new Vector2(1 + s * (svWidth - 3), 1 + (1 - v) * (svHeight - 3));
        Draw.Point(svPos - Vector2.UnitX, Color.Black);
        Draw.Point(svPos + Vector2.UnitX, Color.Black);
        Draw.Point(svPos - Vector2.UnitY, Color.Black);
        Draw.Point(svPos + Vector2.UnitY, Color.Black);

        Draw.Rect(position + new Vector2(1, svHeight), Width - 2, colorPreviewSize - 3, Value);

        base.Render(position);
    }

    private static void HSV(Color color, out float hue, out float saturation, out float value) {
        float r = (float)color.R / 255;
        float g = (float)color.G / 255;
        float b = (float)color.B / 255;

        float min = Math.Min(Math.Min(r, g), b);
        float max = Math.Max(Math.Max(r, g), b);
        float d = max - min;

        hue = 0f;
        if (max == r)
            hue = 60f * ((g - b) / d % 6f);
        else if (max == g)
            hue = 60f * ((b - r) / d + 2f);
        else if (max == b)
            hue = 60f * ((r - g) / d + 4f);
        hue /= 360f;

        saturation = max == 0 ? 0 : d / max;
        value = max;
    }
}