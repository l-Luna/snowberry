using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI;

public class UIValueTextField<T> : UITextField {
    public new Color Line = Color.Teal;
    public new Color LineSelected = Color.LimeGreen;
    public Color ErrLine = Calc.HexToColor("db2323");
    public Color ErrLineSelected = Calc.HexToColor("ffbb33");

    public bool Error;
    private float errLerp;

    public Action<T> OnValidInputChange;
    public new T Value { get; private set; }

    private static readonly char[] integerChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-' };
    private static readonly char[] floatChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '.', ',', 'e' };

    public UIValueTextField(Font font, int width, string input = "")
        : base(font, width, input) {
        AllowedCharacters = Type.GetTypeCode(typeof(T)) switch {
            TypeCode.Int32 or TypeCode.Int64 => integerChars,
            TypeCode.Single or TypeCode.Double => floatChars,
            _ => null
        };

        GrabsClick = true;
    }

    protected override void Initialize() {
        base.Initialize();
        errLerp = Error ? 1f : 0f;
    }

    public override void Update(Vector2 position = default) {
        errLerp = Calc.Approach(errLerp, Error ? 1f : 0f, Engine.DeltaTime * 7f);
        base.Line = Color.Lerp(Line, ErrLine, errLerp);
        base.LineSelected = Color.Lerp(LineSelected, ErrLineSelected, errLerp);

        base.Update(position);
    }

    protected override void OnInputUpdate(string input) {
        base.OnInputUpdate(input);
        try {
            Value = (T)Convert.ChangeType(input, typeof(T));
            OnValidInputChange?.Invoke(Value);
            Error = false;
        } catch {
            Value = default;
            Error = true;
        }
    }
}