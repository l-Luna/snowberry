using System;
using Celeste.Mod;
using Monocle;

namespace Snowberry;

public static class Keyboard {

    private static Action<char> onInput;
    private static bool commandsEnabled = true;

    public static Action<char> OnInput {
        get => onInput;
        set {
            if (onInput != null)
                TextInput.OnInput -= onInput;
            else
                commandsEnabled = Engine.Commands.Enabled;

            onInput = value;

            if (onInput != null) {
                TextInput.OnInput += onInput;
                Engine.Commands.Enabled = false;
            } else
                Engine.Commands.Enabled = commandsEnabled;
        }
    }

}