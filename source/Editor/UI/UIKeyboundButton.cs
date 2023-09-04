﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Snowberry.Editor.UI;

public class UIKeyboundButton : UIButton{

    public bool Ctrl, Alt, Shift;
    public Keys Key;

    public UIKeyboundButton(int width, int height, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8) : base(width, height, spaceX, spaceY, minWidth, minHeight){}
    public UIKeyboundButton(string text, Font font, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8) : base(text, font, spaceX, spaceY, minWidth, minHeight){}
    public UIKeyboundButton(MTexture icon, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8) : base(icon, spaceX, spaceY, minWidth, minHeight){}
    public UIKeyboundButton(Action<Vector2, Color> action, int icoWidth, int icoHeight, int spaceX = 0, int spaceY = 0, int minWidth = 6, int minHeight = 8) : base(action, icoWidth, icoHeight, spaceX, spaceY, minWidth, minHeight){}

    public override void Update(Vector2 position = default) {
        base.Update(position);

        if ((!Ctrl || MInput.Keyboard.Check(Keys.LeftControl, Keys.RightControl))
        && (!Alt || MInput.Keyboard.Check(Keys.LeftAlt, Keys.RightAlt))
        && (!Shift || MInput.Keyboard.Check(Keys.LeftShift, Keys.RightShift))) {
            // when you first press the button, run on-press action
            if(MInput.Keyboard.Pressed(Key))
                Pressed();
            // as long as its held, appear visually held
            if(MInput.Keyboard.Check(Key))
                pressed = true;
            if(MInput.Keyboard.Released(Key))
                pressed = false;
        }
    }
}