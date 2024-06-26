﻿using Celeste.Mod;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Snowberry.Editor;
using Snowberry.UI;

namespace Snowberry;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[MeansImplicitUse]
public class PluginAttribute(string Name) : Attribute {
    internal readonly string Name = Name;
}

[AttributeUsage(AttributeTargets.Field)]
[MeansImplicitUse]
public class OptionAttribute(string optionName) : Attribute {
    internal readonly string Name = optionName;
}

public abstract class Plugin {
    public PluginInfo Info { get; internal set; }
    public string Name { get; internal set; }

    /// Called before a property is set.
    public event Action<string, object> OnPropChange;

    /// Holds attributes not recognized as plugin options.
    public Dictionary<string, object> UnknownAttrs { get; } = new();

    // overriden by generic plugins
    public virtual void Set(string option, object value) {
        if (Info.Options.TryGetValue(option, out PluginOption f)) {
            try {
                // TODO: this is stupid
                //  - really StrToObject should (and does!) handle all of thesec
                object v;
                if (f.FieldType == typeof(char) && value is not char)
                    v = value.ToString()[0];
                else if (f.FieldType == typeof(Color) && value is not Color)
                    v = Monocle.Calc.HexToColor(value.ToString());
                else if (f.FieldType == typeof(Tileset) && value is not Tileset)
                    v = Tileset.ByKey(value.ToString()[0], false);
                else
                    v = value is string str ? StrToObject(f.FieldType, str) : Convert.ChangeType(value, f.FieldType);
                OnPropChange?.Invoke(option, v);
                f.SetValue(this, v);
            } catch (ArgumentException e) {
                Snowberry.Log(LogLevel.Warn,
                    $"Tried to set field {option} to an invalid value {value} ({value?.GetType().FullName ?? "null"})");
                Snowberry.Log(LogLevel.Warn, e.ToString());
            }
        } else
            UnknownAttrs[option] = value;
    }

    public virtual object Get(string option) {
        object o;
        if (Info.Options.TryGetValue(option, out PluginOption f))
            o = f.GetValue(this);
        else
            UnknownAttrs.TryGetValue(option, out o); // sets o to null if not present

        return ObjectToStr(o);
    }

    public string GetTooltipFor(string option) =>
        Info.Options.TryGetValue(option, out PluginOption f) ? f.Tooltip : null;

    public virtual (UIElement, int height)? CreateOptionUi(string optionName) => null;

    // editing of a specific property
    public UndoRedo.Snapshotter SnapshotOption(string option) => new PropertySnapshotter(this, option);

    private record PropertySnapshotter(Plugin p, string option) : UndoRedo.Snapshotter<object> {
        public object Snapshot() => p.Get(option);

        public void Apply(object t) {
            if (!Equals(p.Get(option), t))
                p.Set(option, t);
        }
    }

    public void SnapshotAndSet(string option, object value) {
        if (Equals(value, Get(option)))
            return;

        UndoRedo.BeginAction("edit plugin option", SnapshotOption(option));
        Set(option, value);
        UndoRedo.CompleteAction();
    }

    public void SnapshotWeakAndSet(string option, object value) {
        if (Equals(value, Get(option)))
            return;

        if (UndoRedo.ViewInProgress() is not { /* non-null */ } u
            || !u.HasMatching(sn => sn is PropertySnapshotter ps && ps.p == this && ps.option == option))
            UndoRedo.BeginWeakAction("edit plugin option", SnapshotOption(option));

        Set(option, value);
    }

    public static object StrToObject(Type targetType, string raw){
        if(targetType.IsEnum)
            try {
                return Enum.Parse(targetType, raw);
            } catch {
                return null;
            }

        if(targetType == typeof(Color))
            return Monocle.Calc.HexToColor(raw);
        if(targetType == typeof(char))
            return raw[0];
        if(targetType == typeof(Tileset))
            return Tileset.ByKey(raw[0], false);
        if(targetType == typeof(bool))
            return raw.Equals("true", StringComparison.InvariantCultureIgnoreCase);

        try {
            return Convert.ChangeType(raw, targetType);
        } catch (Exception e) {
            Snowberry.Log(LogLevel.Error,
                $"""
                 Attempted invalid conversion of string "{raw}" into type "{targetType.FullName}"!
                 {e}
                 """);
            return Util.Default(targetType);
        }
    }

    public static object ObjectToStr(object obj) => obj switch {
        Color color => color.IntoRgbString(),
        Enum => obj.ToString(),
        char ch => ch.ToString(),
        Tileset ts => ts.Key.ToString(),
        null => null, // good to be explicit
        _ => obj,
    };
}

public interface DictBackedPlugin {
    public Dictionary<string, object> Attrs { get; }
}