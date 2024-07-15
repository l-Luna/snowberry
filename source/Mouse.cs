﻿using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry;

public static class Mouse {
    public static bool InBounds { get; internal set; }
    public static Vector2 Screen { get; internal set; }
    public static Vector2 ScreenLast { get; internal set; }

    public static Vector2 World { get; internal set; }
    public static Vector2 WorldLast { get; internal set; }

    public static bool IsFocused { get; internal set; }

    public static DateTime LastClick { get; internal set; }
    public static bool IsDoubleClick => IsFocused && MInput.Mouse.PressedLeftButton && DateTime.Now < LastClick.AddMilliseconds(200);
}