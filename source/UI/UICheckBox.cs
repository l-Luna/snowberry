using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI;

public class UICheckBox : UIButton {
    public bool Checked;
    private float checkLerp;

    new public Action<bool> OnPress;

    new public Color FG = Calc.HexToColor("db2323");
    new public Color BG = Calc.HexToColor("1d1d21");
    public Color CheckedFG = Color.LimeGreen;
    public Color CheckedBG = Calc.HexToColor("1d1d21");

    public string Enabled = "✓";
    public string Disabled = "×";

    public UICheckBox(int size, bool check = false)
        : base("", Fonts.Regular, size, size) {
        Checked = check;
        GrabsClick = true;
    }

    protected override void Initialize() {
        base.Initialize();
        SetText(Checked ? Enabled : Disabled);
        checkLerp = Checked ? 1f : 0f;
    }

    public override void Update(Vector2 position = default) {
        checkLerp = Calc.Approach(checkLerp, Checked ? 1f : 0f, Engine.DeltaTime * 7f);
        base.FG = Color.Lerp(FG, CheckedFG, checkLerp);
        base.BG = Color.Lerp(BG, CheckedBG, checkLerp);

        base.Update(position);
    }

    protected override void Pressed() {
        base.Pressed();
        Checked = !Checked;
        OnPress?.Invoke(Checked);
        SetText(Checked ? Enabled : Disabled);
    }
}