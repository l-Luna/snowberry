﻿using System;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.UI.Controls;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Snowberry.UI;

// Segmented UIButton that hovers and clicks different entries separately
public class UIDropdown : UIElement {
    public class DropdownEntry(string label, Action onPress, Action onAltPress = null) {
        public string Label = label;
        public MTexture Icon;
        public Action OnPress = onPress, OnAlternatePress = onAltPress;
        public object Tag;
        public Color FG = UIButton.DefaultFG;
        public Color BG = UIButton.DefaultBG;
        public Color PressedFG = UIButton.DefaultPressedFG;
        public Color PressedBG = UIButton.DefaultPressedBG;
        public Color HoveredFG = UIButton.DefaultHoveredFG;
        public Color HoveredBG = UIButton.DefaultHoveredBG;
    }

    public int Limit = 25;

    private readonly Vector2 spacing = new(4);
    private Font font;
    private float[] lerps;
    private int hoverIdx = -1, pressIdx = -1;

    private int offset = 0; // scrolling etc

    public readonly List<DropdownEntry> Entries = new();

    private readonly MTexture
        top,
        bottom,
        topFill,
        bottomFill,
        mid;

    public UIDropdown(Font font, params DropdownEntry[] entries) {
        Entries.AddRange(entries);
        lerps = new float[entries.Length];
        this.font = font;

        MTexture full = GFX.Gui["Snowberry/button"];
        top = full.GetSubtexture(0, 0, 3, 4);
        topFill = full.GetSubtexture(2, 0, 1, 4);
        bottom = full.GetSubtexture(0, 5, 3, 3);
        bottomFill = full.GetSubtexture(2, 5, 1, 4);
        mid = full.GetSubtexture(0, 4, 2, 1);

        float maxWidth = 6;
        foreach (var entry in entries) {
            var area = font.Measure(entry.Label);
            maxWidth = Math.Max(maxWidth, area.X + (entry.Icon != null ? entry.Icon.Width + 3 : 0)) + 3;
            Height += (int)Math.Max(area.Y, entry.Icon?.Height ?? 0);
        }

        Width = (int)maxWidth + 6;
        Height += 8;

        GrabsScroll = true;
    }

    public static UIDropdown OfEnum<T>(Font font, Action<T> onSelect) where T : Enum {
        return OfEnum(font, typeof(T), v => onSelect((T)v));
    }

    public static UIDropdown OfEnum(Font font, Type t, Action<object> onSelect) {
        if (!t.IsEnum)
            throw new InvalidCastException("Cannot use UIDropdown.OfEnum on non-enum type!");

        var values = t
            .GetEnumValues()
            .Cast<object>()
            .Select(v => Convert.ChangeType(v, t))
            .Select(v => new DropdownEntry(v.ToString(), () => onSelect(v)))
            .ToArray();

        return new UIDropdown(font, values);
    }

    public override void Update(Vector2 position = default) {
        base.Update(position);

        hoverIdx = FindHoverIdx(position);
        bool hovering = hoverIdx != -1;

        if (hovering && (ConsumeLeftClick() || ConsumeAltClick()))
            pressIdx = hoverIdx;
        else if (hovering && pressIdx != -1) {
            if (ConsumeAltClick(pressed: false, released: true)) {
                Entries[pressIdx].OnAlternatePress?.Invoke();
                pressIdx = -1;
                RemoveSelf();
            } else if (ConsumeLeftClick(pressed: false, released: true)) {
                Entries[pressIdx].OnPress?.Invoke();
                pressIdx = -1;
                RemoveSelf();
            }
        } else if (MInput.Mouse.ReleasedLeftButton || MInput.Mouse.ReleasedRightButton)
            RemoveSelf();

        for (int i = 0; i < lerps.Length; i++)
            lerps[i] = Calc.Approach(lerps[i], pressIdx == i ? 1f : 0f, Engine.DeltaTime * 20f);

        int diff = Entries.Count - Limit;
        if (diff > 0) {
            if (new Rectangle((int)position.X, (int)position.Y, Width, Height).Contains(Mouse.Screen.ToPoint())) {
                offset = MInput.Mouse.WheelDelta switch {
                    < 0 => Math.Min(offset + 1, diff),
                    > 0 => Math.Max(offset - 1, 0),
                    _ => offset
                };
            }
        }
    }

    public int FindHoverIdx(Vector2 position) {
        var ret = -1;
        var mouse = Mouse.Screen.ToPoint();

        int realCount = Math.Min(Entries.Count, Limit);
        for (int i = 0; i < realCount; i++)
            if (new Rectangle((int)position.X + 1, (int)(position.Y + i * (font.LineHeight + 4)) + 1 + 4, Width - 2, font.LineHeight + 4).Contains(mouse))
                ret = i;

        return ret == -1 ? -1 : ret + offset;
    }

    public float YPosFor(int i) => (i - offset) * (font.LineHeight + 4);

    public override void Render(Vector2 position = default) {
        base.Render(position);

        // draw top
        var defaultColor = ColorForEntry(0);
        top.Draw(new Vector2(position.X, position.Y), Vector2.Zero, defaultColor);
        topFill.Draw(new Vector2(position.X + 3, position.Y), Vector2.Zero, defaultColor, new Vector2(Width - 6, 1));
        top.Draw(new Vector2(position.X + Width, position.Y), Vector2.Zero, defaultColor, new Vector2(-1, 1));
        // draw each entry
        int realCount = Math.Min(Entries.Count, Limit);
        for (int i = offset; i < offset + realCount; i++) {
            DropdownEntry entry = Entries[i];
            var ePos = position + Vector2.UnitY * YPosFor(i);
            var press = pressIdx == i ? 1 : 0;
            var bg = ColorForEntry(i);
            float h = font.Measure(entry.Label).Y;
            float textOffset = 0;
            if (entry.Icon != null) {
                h = Math.Max(h, entry.Icon.Height);
                textOffset = entry.Icon.Width + 3;
            }
            mid.Draw(new Vector2(ePos.X, ePos.Y + h - 4), Vector2.Zero, bg);
            mid.Draw(new Vector2(ePos.X + Width, ePos.Y + h - 4), Vector2.Zero, bg, new Vector2(-1, 1));
            Draw.Rect(new Vector2(ePos.X, ePos.Y + 4), Width, h + 4, Color.Black);
            Draw.Rect(new Vector2(ePos.X + 1, ePos.Y + 4), Width - 2, h + 4, bg);
            Color fg = Color.Lerp(hoverIdx == i ? entry.HoveredFG : entry.FG, entry.PressedFG, lerps[i]);
            entry.Icon?.Draw(ePos + new Vector2(4 + press, entry.Icon.Height));
            font.Draw(entry.Label, ePos + new Vector2(4 + press + textOffset, 5), Vector2.One, fg);
        }

        // draw bottom
        defaultColor = ColorForEntry(realCount - 1);
        var h2 = YPosFor(realCount + offset) + 4;
        bottom.Draw(new Vector2(position.X, position.Y + h2), Vector2.Zero, defaultColor);
        bottomFill.Draw(new Vector2(position.X + 3, position.Y + h2), Vector2.Zero, defaultColor, new Vector2(Width - 6, 1));
        bottom.Draw(new Vector2(position.X + Width, position.Y + h2), Vector2.Zero, defaultColor, new Vector2(-1, 1));

        // draw scroll arrows
        int diff = Entries.Count - Limit;
        if (diff > 0) {
            if (offset > 0)
                font.Draw("\u2191", position + new Vector2(Width - 4, 2), new Vector2(1), new Vector2(1, 0), Color.White);
            if (offset < diff)
                font.Draw("\u2193", position + new Vector2(Width - 4, h2 - 2), new Vector2(1), new Vector2(1, 1), Color.White);
        }
    }

    public Color ColorForEntry(int index) {
        if (index >= Entries.Count)
            return UIButton.DefaultBG;

        DropdownEntry e = Entries[index];
        return Color.Lerp(hoverIdx == index ? e.HoveredBG : e.BG, e.PressedBG, lerps[index]);
    }
}