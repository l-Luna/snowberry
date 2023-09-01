using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Snowberry.Editor.UI;

public class UIToolbar : UIElement {
    public Color BG = Calc.HexToColor("202929");

    private static readonly Color BtnFG = Calc.HexToColor("70a9c2");
    private static readonly Color BtnBG = Calc.HexToColor("0b314f");
    private static readonly Color BtnPressedFG = Calc.HexToColor("b9d8e5");
    private static readonly Color BtnPressedBG = Calc.HexToColor("1e4c6f");
    private static readonly Color BtnHoveredFG = Calc.HexToColor("8abacf");
    private static readonly Color BtnHoveredBG = Calc.HexToColor("123e5f");
    private static readonly Color BtnSelectedFG = Calc.HexToColor("ace6dc");
    private static readonly Color BtnSelectedBG = Calc.HexToColor("213c38");

    public int CurrentTool;

    protected List<UIButton> toolButtons = new();

    public UIToolbar(Editor editor) {
        CurrentTool = 0;
        BG.A = 185;

        for (int i = 0; i < Tool.Tools.Count; i++) {
            Tool tool = Tool.Tools[i];
            var toolButton = new UIButton(tool.GetName(), Fonts.Regular, 6, 6);
            int copy = i; // thanks lambdas, very cool
            toolButton.OnPress = () => editor.SwitchTool(copy);
            toolButton.Position = new Vector2(Position.X + Width + 3, Position.Y + 5);
            Width += toolButton.Width + 3;
            Add(toolButton);
            toolButtons.Add(toolButton);
            if (Height < toolButton.Height)
                Height = toolButton.Height;
        }

        Height += 8;
        Width += 6;
    }

    public override void Update(Vector2 position = default) {
        base.Update(position);
        for (int i = 0; i < toolButtons.Count; i++) {
            UIButton button = toolButtons[i];
            if (i == CurrentTool) {
                button.FG = button.PressedFG = button.HoveredFG = BtnSelectedFG;
                button.BG = button.PressedBG = button.HoveredBG = BtnSelectedBG;
            } else {
                button.FG = BtnFG;
                button.PressedFG = BtnPressedFG;
                button.HoveredFG = BtnHoveredFG;
                button.BG = BtnBG;
                button.PressedBG = BtnPressedBG;
                button.HoveredBG = BtnHoveredBG;
            }
        }

        // Ctrl-any digit to select that tool
        if (Editor.Instance.CanTypeShortcut())
            if (MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl))
                for (var idx = 0; idx < Util.DigitKeys.Count; idx++)
                    if (MInput.Keyboard.Pressed(Util.DigitKeys[idx])) {
                        if (Tool.Tools.Count > idx)
                            Editor.Instance.SwitchTool(idx);
                        break;
                    }
    }

    public override void Render(Vector2 position = default) {
        Rectangle rect = new Rectangle((int)position.X, (int)position.Y, Width, Height);
        Draw.Rect(rect, BG);
        base.Render(position);
    }
}