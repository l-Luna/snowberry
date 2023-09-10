using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI;

public class UIScrollPane : UIElement {
    public Color BG = Calc.HexToColor("202929") * (185 / 255f);
    public int BottomPadding = 0, TopPadding = 0;
    public bool ShowScrollBar = true;
    public bool Vertical = true;
    public float Scroll = 0;

    public UIScrollPane() {
        Background = BG;
        GrabsScroll = true;
        GrabsClick = true;
    }

    public override void Render(Vector2 position = default) {
        Rectangle rect = new Rectangle((int)position.X, (int)position.Y, Width, Height);
        // render the BG ourselves
        if (Background.HasValue)
            Draw.Rect(rect, Background.Value);

        DrawUtil.WithinScissorRectangle(rect, () => {
            base.Render(position + ScrollOffset());

            if (ShowScrollBar) {
                var hilo = HighLow();
                float minScroll = Height - hilo.lo, maxScroll = -hilo.hi;
                if (minScroll < maxScroll) { // otherwise, we can't scroll at all
                    float offscreen = (maxScroll - minScroll);
                    // we would like to keep the blank area = offscreen area, so dragging appears to linearly move things
                    // until we get to very small sizes, then we need to just rely on a minimum
                    float thumbSize = Math.Max(Height - offscreen, 12);
                    Draw.Rect(position + new Vector2(Width - 4, (Height - thumbSize) * (1 - (Scroll - minScroll) / offscreen)), 3, thumbSize, Color.DarkCyan * 0.5f);
                }
            }
        });
    }

    private Vector2 ScrollOffset() {
        return (Vertical ? Vector2.UnitY : Vector2.UnitX) * Scroll;
    }

    public override void Update(Vector2 position = default) {
        bool hovered = Bounds.Contains((int)Mouse.Screen.X, (int)Mouse.Screen.Y);

        // pretend that the mouse has already been clicked if the mouse is outside of the scroll pane's bounds
        bool mouseClicked = UIScene.Instance.MouseClicked;
        UIScene.Instance.MouseClicked = !hovered || mouseClicked;
        base.Update(position + ScrollOffset());
        UIScene.Instance.MouseClicked = mouseClicked;

        if (hovered)
            ScrollBy(MInput.Mouse.WheelDelta);

        // TODO: make optional? or into UIElement behaviour?
        // fit to parent's height if not set, like for the tile brush panel
        Height = Height == 0 ? Parent?.Height ?? 0 : Height;
    }

    public override Vector2 BoundsOffset() => ScrollOffset();

    protected override bool RenderBg() => false;

    public void ScrollBy(float amount) {
        // note that Scroll is almost always negative, hence the maximum value is always negative unless some element has negative coords
        var hilo = HighLow();
        Scroll += amount;
        Scroll = Clamp(Scroll, Height - hilo.lo, -hilo.hi);
    }

    public (float hi, float lo) HighLow() {
        UIElement low = null, high = null;
        foreach(var item in Children.Where(item => item.Visible)){
            if (low == null || item.Position.Y > low.Position.Y) low = item;
            if (high == null || item.Position.Y < high.Position.Y) high = item;
        }

        return (high != null ? high.Position.Y - TopPadding : 0, low != null ? low.Position.Y + low.Height + BottomPadding : 0);
    }

    // MathHelper.clamp, but we check constraints the other way around,
    // so scrollpanes with few elements get stuck to the top instead of bottom
    public static float Clamp(float value, float min, float max) {
        value = value < min ? min : value;
        value = value > max ? max : value;
        return value;
    }
}