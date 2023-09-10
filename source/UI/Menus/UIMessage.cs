using System;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Snowberry.UI.Menus;

public class UIMessage : UIElement {
    private class Msg {
        public UIElement Element { get; private set; }
        public Vector2 DisplayJustification { get; private set; }
        public Vector2 HiddenJustification { get; private set; }

        public Msg(UIElement element, Vector2 displayJustify, Vector2 hiddenJustify) {
            Element = element;
            DisplayJustification = displayJustify;
            HiddenJustification = hiddenJustify;
        }

        public void UpdateElement(int w, int h, float lerp) {
            Element.Position = (new Vector2(w, h) * Vector2.Lerp(HiddenJustification, DisplayJustification, lerp)).Round();
        }
    }

    private readonly List<Msg> msgs = new();

    private float lerp;
    public bool Shown;

    public new void Clear() {
        base.Clear(); msgs.Clear();
    }

    public void AddElement(UIElement element, float justifyX, float justifyY, float hiddenJustifyX, float hiddenJustifyY) {
        Add(element);
        var msg = new Msg(element, new Vector2(justifyX, justifyY), new Vector2(hiddenJustifyX, hiddenJustifyY));
        msgs.Add(msg);
        msg.UpdateElement(Width, Height, Ease.ExpoOut(lerp));
    }

    public override void Update(Vector2 position = default) {
        base.Update(position);

        lerp = Calc.Approach(lerp, Shown.Bit(), Engine.DeltaTime * 2f);
        float ease = Ease.ExpoOut(lerp);

        foreach (Msg msg in msgs) {
            msg.UpdateElement(Width, Height, ease);
        }

        if (MInput.Keyboard.Check(Keys.Escape))
            Shown = false;
    }

    public override void Render(Vector2 position = default) {
        Draw.Rect(position, Width, Height, Color.Black * lerp * 0.75f);
        base.Render(position);
    }

    public static UIElement YesAndNoButtons(Action yesPress = null, Action noPress = null, float ox = 0f, float oy = 0f, float jx = 0f, float jy = 0f) {
        UIButton yes = new UIButton(Dialog.Clean("SNOWBERRY_MAINMENU_YES"), Fonts.Regular, 4, 6) {
            FG = Util.Colors.White,
            BG = Util.Colors.Blue,
            PressedBG = Util.Colors.White,
            PressedFG = Util.Colors.Blue,
            HoveredBG = Util.Colors.DarkBlue,
            OnPress = yesPress,
        };
        UIButton no = new UIButton(Dialog.Clean("SNOWBERRY_MAINMENU_NO"), Fonts.Regular, 4, 6) {
            FG = Util.Colors.White,
            BG = Util.Colors.Red,
            PressedBG = Util.Colors.White,
            PressedFG = Util.Colors.Red,
            HoveredBG = Util.Colors.DarkRed,
            Position = new Vector2(yes.Position.X + yes.Width + 4, yes.Position.Y),
            OnPress = noPress,
        };

        var buttons = Regroup(yes, no);

        Vector2 offset = new Vector2(buttons.Width * -jx + ox, buttons.Height * -jy + oy);
        yes.Position += offset;
        no.Position += offset;

        return buttons;
    }
}