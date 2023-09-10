using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Snowberry.Editor;

namespace Snowberry.UI.Menus;

public class UIPluginOptionList : UIElement {
    public class UIOption : UIElement {
        public readonly UIElement Input;
        private readonly UILabel Label;

        public UIOption(string name, UIElement input, string tooltip = default) {
            Input = input;

            Add(Label = new UILabel($"{name} : ") {
                FG = Color.Gray,
                LabelTooltip = tooltip
            });
            int w = Label.Width + 1;

            if (input != null) {
                Add(input);
                input.Position = Vector2.UnitX * w;
            }

            Width = w + (input?.Width ?? 0);
            Height = Math.Max(Fonts.Regular.LineHeight, input?.Height ?? 0);
        }

        public UIOption WithTooltip(string tooltip) {
            Label.LabelTooltip = tooltip;
            return this;
        }
    }

    public readonly Plugin Plugin;

    public UIPluginOptionList(Plugin plugin) {
        Plugin = plugin;
        Refresh();
    }

    public void Refresh() {
        int l = 0;
        int spacing = 13;
        foreach (var option in Plugin.Info.Options) {
            object value = option.Value.GetValue(Plugin);
            // TODO: this is kind of silly
            if (option.Value.FieldType == typeof(bool)) {
                UIOption ui;
                Add(ui = BoolOption(option.Key, (bool)value, Plugin));
                ui.Position.Y = l;
                l += spacing;
            } else if (option.Value.FieldType == typeof(Color)) {
                UIOption ui;
                Add(ui = ColorOption(option.Key, (Color)value, Plugin));
                ui.Position.Y = l;
                l += 91;
            } else if (option.Value.FieldType == typeof(int)) {
                UIOption ui;
                Add(ui = LiteralValueOption<int>(option.Key, value.ToString(), Plugin));
                ui.Position.Y = l;
                l += spacing;
            } else if (option.Value.FieldType == typeof(long)) {
                UIOption ui;
                Add(ui = LiteralValueOption<long>(option.Key, value.ToString(), Plugin));
                ui.Position.Y = l;
                l += spacing;
            } else if (option.Value.FieldType == typeof(float)) {
                UIOption ui;
                Add(ui = LiteralValueOption<float>(option.Key, value.ToString(), Plugin));
                ui.Position.Y = l;
                l += spacing;
            } else if (option.Value.FieldType == typeof(double)) {
                UIOption ui;
                Add(ui = LiteralValueOption<double>(option.Key, value.ToString(), Plugin));
                ui.Position.Y = l;
                l += spacing;
            } else if (option.Value.FieldType == typeof(Tileset)) {
                UIOption ui;
                Add(ui = TilesetDropdownOption(option.Key, (Tileset)value, Plugin));
                ui.Position.Y = l;
                l += spacing + 6;
            } else if (option.Value.FieldType.IsEnum) {
                UIOption ui;
                Add(ui = DropdownOption(option.Key, option.Value.FieldType, value, Plugin));
                ui.Position.Y = l;
                l += spacing + 6;
            } else {
                UIOption ui;
                Add(ui = StringOption(option.Key, value?.ToString() ?? "", Plugin));
                ui.Position.Y = l;
                l += spacing;
            }
            Height = l;
            Width = Math.Max(Children.Max(k => k.Width), Width);
        }
    }

    public static UIOption StringOption(string name, string value, Action<string> onChange, int width = 80) {
        var checkbox = new UITextField(Fonts.Regular, width, value) {
            OnInputChange = str => onChange?.Invoke(str)
        };
        return new UIOption(name, checkbox);
    }

    public static UIOption StringOption(string name, string value, Plugin plugin, int width = 80) {
        var checkbox = new UITextField(Fonts.Regular, width, value) {
            OnInputChange = str => plugin.Set(name, str)
        };
        return new UIOption(name, checkbox, plugin.GetTooltipFor(name));
    }

    public static UIOption LiteralValueOption<T>(string name, string value, Action<T> onChange, int width = 80) {
        var checkbox = new UIValueTextField<T>(Fonts.Regular, width, value) {
            OnValidInputChange = v => onChange?.Invoke(v)
        };
        return new UIOption(name, checkbox);
    }

    public static UIOption LiteralValueOption<T>(string name, string value, Plugin plugin, int width = 80) {
        var checkbox = new UIValueTextField<T>(Fonts.Regular, width, value) {
            OnValidInputChange = v => plugin.Set(name, v)
        };
        return new UIOption(name, checkbox, plugin.GetTooltipFor(name));
    }

    public static UIOption BoolOption(string name, bool value, Action<bool> onChange) {
        var checkbox = new UICheckBox(-1, value) {
            OnPress = b => onChange?.Invoke(b)
        };
        return new UIOption(name, checkbox);
    }

    public static UIOption BoolOption(string name, bool value, Plugin plugin) {
        var checkbox = new UICheckBox(-1, value) {
            OnPress = b => plugin.Set(name, b)
        };
        return new UIOption(name, checkbox, plugin.GetTooltipFor(name));
    }

    public static UIOption ColorOption(string name, Color value, Action<Color> onChange) {
        var colorpicker = new UIColorPicker(100, 80, 16, 12, value) {
            OnColorChange = color => onChange?.Invoke(color)
        };
        return new UIOption(name, colorpicker);
    }

    public static UIOption ColorOption(string name, Color value, Plugin plugin) {
        var colorpicker = new UIColorPicker(100, 80, 16, 12, value) {
            OnColorChange = color => plugin.Set(name, color)
        };
        return new UIOption(name, colorpicker, plugin.GetTooltipFor(name));
    }

    public static UIOption DropdownOption<T>(string name, T value, Action<T> onChange) where T : Enum {
        return DropdownOption(name, typeof(T), value, v => onChange((T)v));
    }

    public static UIOption DropdownOption(string name, Type t, object value, Action<object> onChange, string tooltip = default) {
        UIButton button = null;
        button = new UIButton(value + " \uF036", Fonts.Regular, 2, 2) {
            OnPress = () => {
                var dropdown = UIDropdown.OfEnum(Fonts.Regular, t, it => {
                    onChange?.Invoke(it);
                    button.SetText(it + " \uF036");
                });
                dropdown.Position = button.GetBoundsPos() + Vector2.UnitY * (button.Height + 2) - Editor.Editor.Instance.ToolPanel.GetBoundsPos();
                Editor.Editor.Instance.ToolPanel.Add(dropdown);
            }
        };

        return new UIOption(name, button, tooltip);
    }

    public static UIOption DropdownOption(string name, Type t, object value, Plugin plugin) {
        return DropdownOption(name, t, value, v => plugin.Set(name, v), plugin.GetTooltipFor(name));
    }

    public static UIOption TilesetDropdownOption(string name, Tileset value, Plugin plugin) {
        UIButton button = null;
        button = new UIButton(value.Name + " \uF036", Fonts.Regular, 2, 2) {
            OnPress = () => {
                var dropdown = new UIDropdown(Fonts.Regular, Tileset.FgTilesets
                    .Where(ts => ts.Key != '0')
                    .Select(ts => new UIDropdown.DropdownEntry(ts.Name, () => {
                        plugin.Set(name, ts.Key);
                        button.SetText(ts.Name + " \uF036");
                    }) {
                        Icon = ts.Tile.Tiles[0, 0]
                    }).ToArray()) {
                    Position = button.GetBoundsPos() + Vector2.UnitY * (button.Height + 2) - Editor.Editor.Instance.ToolPanel.GetBoundsPos()
                };

                Editor.Editor.Instance.ToolPanel.Add(dropdown);
            }
        };
        // TODO: give the button an icon as well

        return new UIOption(name, button, plugin.GetTooltipFor(name));
    }
}