using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.Editor.Tools;
using System.Collections.Generic;
using Snowberry.UI;

namespace Snowberry.Editor;

public abstract class Tool{

    public static IList<Tool> Tools = new List<Tool> {
        new SelectionTool(),
        new TileBrushTool(),
        new RoomTool(),
        new PlacementTool(),
        new StylegroundsTool(),
        new MapInfoTool()
    };

    public static readonly Color LeftSelectedBtnBg = Calc.HexToColor("274292");
    public static readonly Color RightSelectedBtnBg = Calc.HexToColor("922727");
    public static readonly Color BothSelectedBtnBg = Calc.HexToColor("7d2792");

    public abstract string GetName();

    public abstract UIElement CreatePanel(int height);

    public abstract void Update(bool canClick);

    public virtual void RenderScreenSpace(){}

    public virtual void RenderWorldSpace(){}

    public virtual UIElement CreateActionBar() => null;

    public virtual void SuggestCursor(ref MTexture cursor, ref Vector2 justify){}
}