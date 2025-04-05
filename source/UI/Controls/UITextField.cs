using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Snowberry.Editor;

namespace Snowberry.UI.Controls;

public class UITextField : UIElement {

    public bool Selected {
        get => Keyboard.OnInput == OnInput;
        set {
            if (value)
                Keyboard.OnInput = OnInput;
            else if (Keyboard.OnInput == OnInput)
                Keyboard.OnInput = null;
        }
    }


    private bool hovering;
    private int charIndex, selection;

    public Action<string> OnInputChange;
    public string Value { get; private set; }
    public int ValueWidth => widthAtIndex[^1];
    private int[] widthAtIndex;
    public readonly Font Font;

    private float lerp;

    public Color Line = Color.Teal;
    public Color LineSelected = Color.LimeGreen;
    public Color BG = Color.Black * 0.25f;
    public Color BGSelected = Color.Black * 0.5f;
    public Color FG = Calc.HexToColor("f0f0f0");

    private float timeOffset;

    private Keys? repeatKey = null;
    private float repeatCounter = 0;

    public HashSet<char> CharacterWhitelist, CharacterBlacklist;

    public override bool GrabsKeyboard => Selected;

    public UITextField(Font font, int width, string input = "") {
        Font = font;
        UpdateInput(input ?? "null");
        charIndex = selection = Value.Length;

        Width = Math.Max(1, width);
        Height = font.LineHeight;

        GrabsClick = true;
    }

    private void OnInput(char c) {
        if (Engine.Commands.Open)
            return;

        GetSelection(out int a, out int b);

        if (c == '\b' && Value.Length != 0 && !(a == 0 && b == 0)) {
            int next = a == b ? a - 1 : a;
            if (MInput.Keyboard.Check(Keys.LeftControl, Keys.RightControl))
                while (next > 0 && !MustSeparate(Value[next], Value[next - 1]))
                    next -= 1;

            InsertString(next, b);
            selection = charIndex = next;
            timeOffset = Engine.Scene.TimeActive;
        } else if (!char.IsControl(c) && (CharacterWhitelist == null || CharacterWhitelist.Contains(c)) && (CharacterBlacklist == null || !CharacterBlacklist.Contains(c))) {
            UpdateInput(Value[..a] + c + Value[b..]);
            selection = charIndex = a + 1;
            timeOffset = Engine.Scene.TimeActive;
        }
    }

    private void InsertString(int from, int to, string str = null) {
        UpdateInput(Value[..from] + str + Value[to..]);
    }

    public void UpdateInput(string str, bool updateUnderlying = true) {
        Value = str;
        widthAtIndex = new int[Value.Length + 1];
        int w = 0;
        for (int i = 0; i < widthAtIndex.Length - 1; i++) {
            widthAtIndex[i] = w;
            w += (int)Font.Measure(Value[i]).X + 1;
        }

        widthAtIndex[^1] = w;
        if(updateUnderlying)
            OnInputUpdate(Value);
    }

    protected virtual void OnInputUpdate(string input) {
        OnInputChange?.Invoke(input);
    }

    private void GetSelection(out int a, out int b) {
        if (charIndex < selection) {
            a = charIndex;
            b = selection;
        } else if (selection < charIndex) {
            a = selection;
            b = charIndex;
        } else {
            a = b = charIndex;
        }
    }

    private static bool MustSeparate(char at, char previous) {
        return char.GetUnicodeCategory(char.ToLower(at)) != char.GetUnicodeCategory(char.ToLower(previous));
    }

    private int MoveIndex(int step, bool stepByWord) {
        int next = charIndex;

        if (stepByWord) {
            next += step;
            while (next > 0 && next < Value.Length && !MustSeparate(Value[next], Value[next - 1]))
                next += step;
        } else
            next += step;

        return Calc.Clamp(next, 0, Value.Length);
    }

    public override void Update(Vector2 position = default) {
        base.Update(position);

        int mouseX = (int)Mouse.Screen.X;
        int mouseY = (int)Mouse.Screen.Y;
        bool inside = new Rectangle((int)position.X - 1, (int)position.Y - 1, Width + 2, Height + 2).Contains(mouseX, mouseY);

        if (MInput.Mouse.CheckLeftButton) {
            bool click = MInput.Mouse.PressedLeftButton;

            if (click) {
                // don't require consuming the click to deselect, but do require it to select
                if (inside) {
                    if (ConsumeLeftClick())
                        Selected = true;
                } else {
                    Selected = false;
                }
            }

            if (Selected) {
                int i, d = mouseX - (int)position.X + 1;

                for (i = 0; i < widthAtIndex.Length - 1; i++)
                    if (widthAtIndex[i + 1] >= d)
                        break;

                if (i != charIndex) {
                    charIndex = i;
                    if (click)
                        selection = i;
                    timeOffset = Engine.Scene.TimeActive;
                }
            }
        }

        if (Selected) {
            bool shift = MInput.Keyboard.CurrentState[Keys.LeftShift] == KeyState.Down || MInput.Keyboard.CurrentState[Keys.RightShift] == KeyState.Down;
            bool ctrl = MInput.Keyboard.CurrentState[Keys.LeftControl] == KeyState.Down || MInput.Keyboard.CurrentState[Keys.RightControl] == KeyState.Down;

            if (MInput.Keyboard.Pressed(Keys.Escape))
                Selected = false;
            else {
                bool pressedLeft = MInput.Keyboard.Pressed(Keys.Left);
                bool pressedRight = MInput.Keyboard.Pressed(Keys.Right);

                // this is how `Monocle.Commands` implemented it, so,
                // except it's a bit more gracious but *whatever*
                if (repeatKey is { /* non-null */ } key)
                    if (MInput.Keyboard.CurrentState[key] == KeyState.Down) {
                        for (repeatCounter += Engine.RawDeltaTime; repeatCounter >= 0.5; repeatCounter -= 0.033333335f)
                            if (key == Keys.Left)
                                pressedLeft = true;
                            else if (key == Keys.Right)
                                pressedRight = true;
                    } else
                        repeatKey = null;

                if (pressedLeft && repeatKey == null) {
                    repeatKey = Keys.Left;
                    repeatCounter = 0;
                }
                if (pressedRight && repeatKey == null) {
                    repeatKey = Keys.Right;
                    repeatCounter = 0;
                }

                bool moved = false;
                if (moved |= pressedLeft)
                    charIndex = MoveIndex(-1, ctrl);
                else if (moved |= pressedRight)
                    charIndex = MoveIndex(1, ctrl);
                if (moved) {
                    timeOffset = Engine.Scene.TimeActive;
                    if (!shift)
                        selection = charIndex;
                }
            }

            if (ctrl) {
                bool copy = MInput.Keyboard.Pressed(Keys.C), cut = MInput.Keyboard.Pressed(Keys.X);

                if (MInput.Keyboard.Pressed(Keys.A)) {
                    charIndex = Value.Length;
                    selection = 0;
                }

                if (selection != charIndex && (copy || cut)) {
                    GetSelection(out int a, out int b);
                    CopyPaste.Clipboard = Value.Substring(a, b - a);
                    if (cut) {
                        InsertString(a, b);
                        selection = charIndex = a;
                    }
                } else if (MInput.Keyboard.Pressed(Keys.V) && CopyPaste.Clipboard != null) {
                    GetSelection(out int a, out int b);
                    InsertString(a, b, CopyPaste.Clipboard);
                    selection = charIndex = a + CopyPaste.Clipboard.Length;
                    timeOffset = Engine.Scene.TimeActive;
                }
            }
        }

        lerp = Calc.Approach(lerp, Selected ? 1f : 0f, Engine.DeltaTime * 4f);
        hovering = inside;
    }

    protected virtual void DrawText(Vector2 position) {
        Font.Draw(Value, position, Vector2.One, FG);
    }

    public override void Render(Vector2 position = default) {
        base.Render(position);

        Draw.Rect(position, Width, Height, Color.Lerp(BG, BGSelected, hovering && !Selected ? 0.25f : lerp));
        DrawText(position);

        Draw.Rect(position + Vector2.UnitY * Height, Width, 1, Line);
        if (lerp != 0f) {
            float ease = Ease.ExpoOut(lerp);
            Vector2 p = new Vector2(position.X + (1 - ease) * Width / 2f, position.Y + Height);
            Draw.Rect(p, (Width + 1) * ease, 1, Color.Lerp(Line, LineSelected, lerp));
        }

        if (Selected) {
            if ((Engine.Scene.TimeActive - timeOffset) % 1f < 0.5f) {
                Draw.Rect(position + Vector2.UnitX * GetWidthAt(charIndex), 1, Font.LineHeight, FG);
            }

            if (selection != charIndex) {
                int a = GetWidthAt(charIndex), b = GetWidthAt(selection);
                if (a < b)
                    Draw.Rect(position + Vector2.UnitX * a, b - a, Font.LineHeight, Color.Blue * 0.25f);
                else
                    Draw.Rect(position + Vector2.UnitX * b, a - b, Font.LineHeight, Color.Blue * 0.25f);
            }
        }
    }

    private int GetWidthAt(int index) => index < 0 ? 0 : index >= widthAtIndex.Length ? widthAtIndex[^1] : widthAtIndex[index];

    protected override void Uninitialize() {
        base.Uninitialize();

        Selected = false;
    }
}