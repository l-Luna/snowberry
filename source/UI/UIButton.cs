using System;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI;

public class UIButton : UIElement {
    private readonly Vector2 space, minSize;
    private string text;
    private Font font;
    private Action<Vector2, Color> icon;

    public static readonly Color DefaultFG = Calc.HexToColor("f0f0f0");
    public static readonly Color DefaultBG = Calc.HexToColor("1d1d21");
    public static readonly Color DefaultPressedFG = Calc.HexToColor("4e4ea3");
    public static readonly Color DefaultPressedBG = Calc.HexToColor("131317");
    public static readonly Color DefaultHoveredFG = Calc.HexToColor("f0f0f0");
    public static readonly Color DefaultHoveredBG = Calc.HexToColor("18181c");

    public Color FG = Calc.HexToColor("f0f0f0");
    public Color BG = Calc.HexToColor("1d1d21");
    public Color PressedFG = Calc.HexToColor("4e4ea3");
    public Color PressedBG = Calc.HexToColor("131317");
    public Color HoveredFG = Calc.HexToColor("f0f0f0");
    public Color HoveredBG = Calc.HexToColor("18181c");

    public String ButtonTooltip;

    private float lerp;
    protected bool pressed, hovering;

    private readonly MTexture
        top,
        bottom,
        topFill,
        bottomFill,
        mid;
    public bool active = true; //Whether or not the Button is able to be pressed. Currently implemented poorly.
    public Action OnPress, OnRightPress;
    public bool Underline = false, Strikethrough = false;

    private UIButton(int spaceX, int spaceY, int minWidth, int minHeight) {
        MTexture full = GFX.Gui["Snowberry/button"];
        top = full.GetSubtexture(0, 0, 3, 4);
        topFill = full.GetSubtexture(2, 0, 1, 4);
        bottom = full.GetSubtexture(0, 5, 3, 3);
        bottomFill = full.GetSubtexture(2, 5, 1, 4);
        mid = full.GetSubtexture(0, 4, 2, 1);

        minSize = new Vector2(minWidth, minHeight);
        space = new Vector2(spaceX, spaceY);

        GrabsClick = true;
    }

    public UIButton(int width, int height, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8)
        : this(spaceX, spaceY, minWidth, minHeight) {
        SetSize(Math.Max(6, width), Math.Max(8, height));
    }

    public UIButton(string text, Font font, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8)
        : this(spaceX, spaceY, minWidth, minHeight) {
        SetText(text, font);
    }

    public UIButton(MTexture icon, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8)
        : this(spaceX, spaceY, minWidth, minHeight) {
        SetIcon(icon);
        FG = PressedFG = HoveredFG = Color.White;
    }

    public UIButton(Action<Vector2, Color> action, int icoWidth, int icoHeight, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8)
        : this(spaceX, spaceY, minWidth, minHeight) {
        SetIconAction(action, icoWidth, icoHeight);
        FG = PressedFG = HoveredFG = Color.White;
    }

    private void SetSize(int width, int height) {
        Width = (int)Math.Max(width + space.X * 2, Math.Max(6, minSize.X));
        Height = (int)Math.Max(height + space.Y * 2, Math.Max(8, minSize.Y));
    }

    public void SetText(string text, Font font = null, bool stayCentered = false) {
        Vector2 mid = Position + new Vector2(Width, Height) / 2f;
        icon = null;
        this.text = text;
        this.font = font ?? this.font;
        Vector2 size = this.font.Measure(this.text);
        SetSize((int)size.X + 6, (int)size.Y + 3);

        if (stayCentered)
            Position = (mid - new Vector2(Width, Height) / 2f).Round();
    }

    public void SetIcon(MTexture icon) {
        this.icon = (at, color) => icon.Draw(at, Vector2.Zero, color);
        SetSize(icon.Width + 6, icon.Height + 3);
    }

    public void SetIconAction(Action<Vector2, Color> action, int icoWidth, int icoHeight) {
        icon = action;
        SetSize(icoWidth + 6, icoHeight + 3);
    }

    public void ResetBgColors() {
        BG = DefaultBG;
        HoveredBG = DefaultHoveredBG;
        PressedBG = DefaultPressedBG;
    }

    public void ResetFgColors() {
        FG = DefaultFG;
        HoveredFG = DefaultHoveredFG;
        PressedFG = DefaultPressedFG;
    }

    public override void Update(Vector2 position = default) {
        base.Update();

        int mouseX = (int)Mouse.Screen.X;
        int mouseY = (int)Mouse.Screen.Y;
        if (active) {
            hovering = new Rectangle((int)position.X + 1, (int)position.Y + 1, Width - 2, Height - 2).Contains(mouseX, mouseY);

            if (hovering && (ConsumeLeftClick() || ConsumeAltClick()))
                pressed = true;
            else if (hovering && pressed) {
                if (ConsumeAltClick(pressed: false, released: true)) {
                    OnRightPress?.Invoke();
                    pressed = false;
                } else if (ConsumeLeftClick(pressed: false, released: true)) {
                    Pressed();
                    pressed = false;
                }
            }

            if (MInput.Mouse.ReleasedLeftButton || MInput.Mouse.ReleasedRightButton) {
                // we only actually activate if the mouse was released on this button + it's visible,
                // but we still need to make it visually unpress if it's dragged off and released
                pressed = false;
            }

            lerp = Calc.Approach(lerp, pressed ? 1f : 0f, Engine.DeltaTime * 20f);
        }
    }

    protected virtual void Pressed() {
        OnPress?.Invoke();
    }

    public override void Render(Vector2 position = default) {
        base.Render(position);

        int press = pressed ? 1 : 0;

        Color bg = Color.Lerp(hovering ? HoveredBG : BG, PressedBG, lerp);

        top.Draw(new Vector2(position.X, position.Y + press), Vector2.Zero, bg);
        topFill.Draw(new Vector2(position.X + 3, position.Y + press), Vector2.Zero, bg, new Vector2(Width - 6, 1));
        top.Draw(new Vector2(position.X + Width, position.Y + press), Vector2.Zero, bg, new Vector2(-1, 1));

        mid.Draw(new Vector2(position.X, position.Y + Height - 4), Vector2.Zero, bg);
        mid.Draw(new Vector2(position.X + Width, position.Y + Height - 4), Vector2.Zero, bg, new Vector2(-1, 1));
        Draw.Rect(new Vector2(position.X, position.Y + 4 + press), Width, Height - 8, Color.Black);
        Draw.Rect(new Vector2(position.X + 1, position.Y + 4 + press), Width - 2, Height - 8, bg);

        bottom.Draw(new Vector2(position.X, position.Y + Height - 3), Vector2.Zero, bg);
        bottomFill.Draw(new Vector2(position.X + 3, position.Y + Height - 3), Vector2.Zero, bg, new Vector2(Width - 6, 1));
        bottom.Draw(new Vector2(position.X + Width, position.Y + Height - 3), Vector2.Zero, bg, new Vector2(-1, 1));

        Draw.Rect(new Vector2(position.X + 2, position.Y + Height - 4 + press), Width - 4, 1, bg);

        Vector2 at = position + new Vector2(3 + space.X, press + space.Y);
        Color fg = Color.Lerp(hovering ? HoveredFG : FG, PressedFG, lerp);
        if (text != null && font != null) {
            font.Draw(text, at, Vector2.One, fg);
            Vector2 textArea = font.Measure(this.text);
            if (Underline)
                Draw.Rect(at + new Vector2(-2, textArea.Y), textArea.X + 4, 1, FG);
            if (Strikethrough)
                Draw.Rect(at + new Vector2(-2, textArea.Y / 2 + 1), textArea.X + 4, 1, Color.Lerp(FG, Color.Black, 0.25f));
        } else icon?.Invoke(at, fg);
    }

    public override string Tooltip() => ButtonTooltip;
}