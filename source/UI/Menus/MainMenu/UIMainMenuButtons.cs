using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI.Menus.MainMenu; 

public class UIMainMenuButtons : UIElement {
    public Color Color = Util.Colors.White;

    public override void Render(Vector2 position = default) {
        UIMainMenu parent = (UIMainMenu)Parent;
        int right = (int)position.X + Width + 16;
        float ease = Ease.CubeInOut(parent.StateLerp(UIMainMenu.States.Load));

        Draw.Rect(0, 0, right, parent.Height, Util.Colors.DarkGray);
        Draw.Rect(right, 8 + parent.Height / 2 * (1 - ease), 1, ease * (parent.Height - 16), Color * ease);

        base.Render(position);
    }
}