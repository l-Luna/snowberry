﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NLua;

namespace Snowberry.Editor.LoennInterop;

public class LoennEntityPluginInfo : PluginInfo, DefaultedPluginInfo {

    protected readonly LuaTable Plugin;
    protected readonly bool IsTrigger;

    // these properties aren't saved with the rest (handled by snowberry) but affect resizability
    public readonly bool HasWidth, HasHeight;

    public Dictionary<string, object> Defaults = new();

    public LoennEntityPluginInfo(string name, LuaTable plugin, bool isTrigger) : base(name, typeof(LoennEntity), null, CelesteEverest.INSTANCE) {
        Plugin = plugin;
        IsTrigger = isTrigger;

        if (plugin["placements"] is LuaTable placements) {
            if (placements.Keys.OfType<string>().Any(k => k.Equals("data"))) {
                if (placements["data"] is LuaTable data)
                    foreach (var item in data.Keys.OfType<string>())
                        if (!Room.IllegalOptionNames.Contains(item)) {
                            Options[item] = new LoennEntityOption(item, data[item].GetType(), name, isTrigger);
                            Defaults.TryAdd(item, data[item]);
                        } else {
                            HasWidth |= item == "width";
                            HasHeight |= item == "height";
                        }
            } else if (placements.Keys.Count >= 1) {
                foreach (var i in placements.Keys) {
                    if (placements[i] is not LuaTable ptable)
                        continue;
                    if (ptable["data"] is LuaTable data)
                        foreach (var item in data.Keys.OfType<string>())
                            if (!Room.IllegalOptionNames.Contains(item)) {
                                Options[item] = new LoennEntityOption(item, data[item].GetType(), name, isTrigger);
                                Defaults.TryAdd(item, data[item]);
                            } else {
                                HasWidth |= item == "width";
                                HasHeight |= item == "height";
                            }
                }
            }
        }

        // field info may be a function, but we don't dynamically call this
        object fieldInfos = plugin["fieldInformation"];
        if (fieldInfos is LuaFunction fn)
            fieldInfos = fn.Call(LoennEntity.EmptyTable()).FirstOrDefault();
        if (fieldInfos is LuaTable fieldInfosTbl) {
            foreach (var fieldKey in fieldInfosTbl.Keys) {
                if (fieldKey is string fieldName && fieldInfosTbl[fieldKey] is LuaTable fieldInfo) {
                    if (!Room.IllegalOptionNames.Contains(fieldName)) {
                        // fieldType: integer, color, boolean, number, string (default)
                        // minimumValue, maximumValue
                        // validator, valueTransformer, displayTransformer
                        // options, editable
                        // allowXNAColors

                        string fieldTypeName = fieldInfo["fieldType"] as string ?? "string";
                        Type fieldType = fieldTypeName.ToLowerInvariant() switch {
                            "number" => typeof(float),
                            "integer" => typeof(int),
                            "boolean" => typeof(bool),
                            "color" => typeof(Color),
                            "snowberry:tileset" or "ext:tileset" => typeof(Tileset),
                            _ => typeof(string)
                        };

                        LoennEntityOption option = new LoennEntityOption(fieldName, fieldType, name, isTrigger);
                        Options[fieldName] = option;

                        if (fieldInfo["options"] is LuaTable options) {
                            option.Options = new();
                            foreach (object key in options.Keys)
                                if (options[key] is { /* non-null anything */ } v)
                                    option.Options[key as string ?? v.ToString()] = v;
                        }

                        if (fieldInfo["editable"] is false)
                            option.Editable = false;
                    } else {
                        HasWidth |= fieldName == "width";
                        HasHeight |= fieldName == "height";
                    }
                }
            }
        }
    }

    public override T Instantiate<T>() {
        if(typeof(T).IsAssignableFrom(typeof(LoennEntity)))
            return new LoennEntity(name, this, Plugin, IsTrigger) as T;
        return null;
    }

    public bool TryGetDefault(string key, out object value) => Defaults.TryGetValue(key, out value);
}

public class LoennEntityOption(string key, Type t, string tooltip) : PluginOption {

    // store as strings for "simplicity", pass through StrToObject when necessary
    public Dictionary<string, object> Options = null;
    public bool Editable = true;

    public LoennEntityOption(string key, Type t, string entityName, bool isTrigger) : this(key, t, FindTooltip(key, entityName, isTrigger)) {}

    public object GetValue(Plugin from) {
        if (((DictBackedPlugin)from).Attrs.TryGetValue(Key, out object v))
            return v;
        if (from.Info is DefaultedPluginInfo lpi && lpi.TryGetDefault(Key, out var def))
            return def is string str ? Plugin.StrToObject(FieldType, str) : Convert.ChangeType(def, FieldType);
        return Util.Default(FieldType);
    }

    public void SetValue(Plugin on, object value) {
        var attrs = ((DictBackedPlugin)on).Attrs;
        var was = attrs.TryGetValue(Key, out var v) ? v : null;
        attrs[Key] = value;
        if (on is Entity onEntity)
            onEntity.Dirty = was != value;
    }

    public Type FieldType { get; } = t;
    public string Key { get; } = key;
    public string Tooltip { get; } = tooltip;

    private static string FindTooltip(string key, string entityName, bool isTrigger) =>
        LoennPluginLoader.Dialog.TryGetValue($"{(isTrigger ? "triggers" : "entities")}.{entityName}.attributes.description.{key}", out var k) ? k.Key : null;
}