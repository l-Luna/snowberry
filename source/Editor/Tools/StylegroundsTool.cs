﻿using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;
using Snowberry.Editor.LoennInterop;
using Snowberry.UI;
using Snowberry.UI.Controls;
using Snowberry.UI.Layout;
using Snowberry.UI.Menus;

namespace Snowberry.Editor.Tools;

public class StylegroundsTool : Tool {
    public List<UIButton> StylegroundButtons = new();
    public Dictionary<UIButton, Styleground> Stylegrounds = new();
    public int SelectedStyleground = 0;

    private UIButton Add, Delete, MoveUp, MoveDown;

    public override UIElement CreatePanel(int height) {
        StylegroundButtons.Clear();
        Stylegrounds.Clear();

        UIElement panel = new() {
            Width = 180,
            Background = Calc.HexToColor("202929") * (185 / 255f),
            GrabsClick = true,
            GrabsScroll = true,
            Height = height
        };
        UIScrollPane stylegrounds = new() {
            Background = null,
            Width = 180,
            Height = 165,
            Tag = "stylegrounds_list"
        };

        var fgLabel = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_UTIL_FOREGROUND")) {
            FG = Color.DarkKhaki,
            Underline = true
        };
        fgLabel.Position = new Vector2((stylegrounds.Width - fgLabel.Width) / 2, 0);
        stylegrounds.Add(fgLabel);

        int i = 0;
        Font font = Fonts.Regular;
        foreach (var styleground in Editor.Instance.Map.FGStylegrounds) {
            int copy = i;
            UIButton element = new UIButton(styleground.Title(), font, 4, 2) {
                Position = new Vector2(10, i * 20 + 20),
                OnPress = () => {
                    SelectedStyleground = copy;
                    AddStylegroundInfo(panel.NestedChildWithTag<UIElement>("stylegrounds_info"));
                }
            };
            stylegrounds.Add(element);
            StylegroundButtons.Add(element);
            Stylegrounds[element] = styleground;
            i++;
        }

        var bgLabel = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_UTIL_BACKGROUND")) {
            FG = Color.DarkKhaki,
            Underline = true
        };
        bgLabel.Position = new Vector2((stylegrounds.Width - bgLabel.Width) / 2, i * 20 + 40);
        stylegrounds.Add(bgLabel);

        foreach (var styleground in Editor.Instance.Map.BGStylegrounds) {
            int copy = i;
            UIButton element = new UIButton(styleground.Title(), font, 4, 2) {
                Position = new Vector2(10, i * 20 + 60),
                OnPress = () => {
                    SelectedStyleground = copy;
                    AddStylegroundInfo(panel.NestedChildWithTag<UIElement>("stylegrounds_info"));
                }
            };
            stylegrounds.Add(element);
            StylegroundButtons.Add(element);
            Stylegrounds[element] = styleground;
            i++;
        }

        if (SelectedStyleground >= StylegroundButtons.Count) {
            SelectedStyleground = 0;
        }

        UIElement stylebg = UIElement.Regroup(stylegrounds);
        stylebg.Background = Color.White * 0.1f;
        stylebg.Position = Vector2.Zero;
        stylebg.Height += 10;
        stylegrounds.Position = new Vector2(0, 5);
        panel.Add(stylebg);

        UIElement optionsPanel = new();
        optionsPanel.AddRight(Add = new UIButton("+ \uF036", font, 4, 4) {
            // add new styleground
            OnPress = () => {
                Vector2 targetPos = Add.GetBoundsPos() + Vector2.UnitY * (Add.Height + 2);
                UIScene.Instance.Overlay.Add(new UIDropdown(font, PluginInfo.Stylegrounds.Select(k => {
                    // TODO: bweh
                    var label = k.Key;
                    if (k.Value is LoennStylegroundPluginInfo lspi) label = lspi.Title() ?? label;
                    return new UIDropdown.DropdownEntry(
                        font.FitWithSuffix(label, 150),
                        () => {
                            var newSg = k.Value.Instantiate<Styleground>();
                            var selected = SelectedButton();
                            if (selected == null)
                                Bgs().Add(newSg);
                            else {
                                var style = Stylegrounds[selected];
                                if (IsFg(style))
                                    Fgs().Insert(SelectedStyleground, newSg);
                                else
                                    Bgs().Insert(SelectedStyleground - Fgs().Count, newSg);
                            }

                            RefreshPanel();
                        }) {
                        BG = BothSelectedBtnBg,
                        HoveredBG = Color.Lerp(BothSelectedBtnBg, Color.Black, 0.25f),
                        PressedBG = Color.Lerp(BothSelectedBtnBg, Color.Black, 0.5f),
                    };
                }).ToArray()) {
                        Limit = (int)((UIScene.Instance.Overlay.Height - targetPos.Y - 30) / (font.LineHeight + 3)),
                        Width = 170,
                        Position = targetPos
                    });
            }
        }, new Vector2(4));

        optionsPanel.AddRight(Delete = new UIButton("-", font, 4, 4) {
            OnPress = () => {
                UIButton selected = SelectedButton();
                if (selected != null) { // if there are no stylegrounds
                    Bgs().Remove(Stylegrounds[selected]);
                    Fgs().Remove(Stylegrounds[selected]);
                    selected.RemoveSelf();
                    RefreshPanel();
                }
            }
        }, new Vector2(4));

        optionsPanel.AddRight(MoveUp = new UIButton("↑", font, 4, 4) {
            OnPress = () => {
                MoveStyleground(-1);
            }
        }, new Vector2(4));

        optionsPanel.AddRight(MoveDown = new UIButton("↓", font, 4, 4) {
            OnPress = () => {
                MoveStyleground(1);
            }
        }, new Vector2(4));

        optionsPanel.CalculateBounds();
        optionsPanel.Height += 8;
        panel.AddBelow(optionsPanel);
        panel.CalculateBounds(); // for height calculation below

        UIElement stylegroundInfo = new UIScrollPane {
            Tag = "stylegrounds_info",
            Width = 175,
            Height = height - panel.Height,
            Position = new Vector2(5, 0),
            Background = Color.Transparent
        };

        panel.AddBelow(stylegroundInfo);
        AddStylegroundInfo(stylegroundInfo);
        panel.CalculateBounds();
        return panel;
    }

    private void AddStylegroundInfo(UIElement panel) {
        panel.Clear(now: true);
        var selected = SelectedButton();
        // might not have any stylegrounds
        if(selected != null && Stylegrounds.TryGetValue(selected, out var styleground)){
            var offset = Vector2.UnitY * 4;
            panel.Add(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_ONLY_IN"), styleground.OnlyIn, s => styleground.OnlyIn = s));
            panel.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_NOT_IN"), styleground.ExcludeFrom, s => styleground.ExcludeFrom = s), offset);
            panel.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_FLAG"), styleground.Flag, s => styleground.Flag = s), offset);
            panel.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_NOTFLAG"), styleground.NotFlag, s => styleground.NotFlag = s), offset);
            panel.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_FCFLAG"), styleground.ForceFlag, s => styleground.ForceFlag = s), offset);
            panel.AddBelow(UIPluginOptionList.ColorAlphaOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_COLOUR"), styleground.RawColor, styleground.Alpha, (c, f) => {
                styleground.RawColor = c;
                styleground.Alpha = f;
            }), offset);
            panel.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_TAGS"), styleground.Tags, t => styleground.Tags = t), offset);

            panel.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_POS")), offset);
            UIElement posC = new();
            posC.AddRight(UIPluginOptionList.LiteralValueOption("x", styleground.Position.X, val => styleground.Position.X = val, width: 40), new(4, 0));
            posC.AddRight(UIPluginOptionList.LiteralValueOption("y", styleground.Position.Y, val => styleground.Position.Y = val, width: 40), new(15, 0));
            posC.CalculateBounds();
            panel.AddBelow(posC, offset);

            panel.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_SCROLL")), offset);
            UIElement scrollC = new();
            scrollC.AddRight(UIPluginOptionList.LiteralValueOption("x", styleground.Scroll.X, val => styleground.Scroll.X = val, width: 40), new(4, 0));
            scrollC.AddRight(UIPluginOptionList.LiteralValueOption("y", styleground.Scroll.Y, val => styleground.Scroll.Y = val, width: 40), new(15, 0));
            scrollC.CalculateBounds();
            panel.AddBelow(scrollC, offset);

            panel.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_SPEED")), offset);
            UIElement speedC = new();
            speedC.AddRight(UIPluginOptionList.LiteralValueOption("x", styleground.Speed.X, val => styleground.Speed.X = val, width: 40), new(4, 0));
            speedC.AddRight(UIPluginOptionList.LiteralValueOption("y", styleground.Speed.Y, val => styleground.Speed.Y = val, width: 40), new(15, 0));
            speedC.CalculateBounds();
            panel.AddBelow(speedC, offset);

            panel.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_LOOP_X"), styleground.LoopX, l => styleground.LoopX = l), offset);
            panel.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_LOOP_Y"), styleground.LoopY, l => styleground.LoopY = l), offset);

            panel.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_FLIP_X"), styleground.FlipX, l => styleground.FlipX = l), offset);
            panel.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_FLIP_Y"), styleground.FlipY, l => styleground.FlipY = l), offset);

            panel.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_INSTIN"), styleground.InstantIn, l => styleground.InstantIn = l), offset);
            panel.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_STYLEGROUNDS_OPTS_INSTOUT"), styleground.InstantOut, l => styleground.InstantOut = l), offset);

            // TODO: dreaming only?

            panel.AddBelow(new UIPluginOptionList(styleground), offset * 2);
        }
    }

    private void MoveStyleground(int by) {
        UIButton selected = SelectedButton();
        if (selected != null) {
            var style = Stylegrounds[selected];
            if (IsFg(style)) {
                int idx = Fgs().IndexOf(style);
                if (idx + by < 0)
                    return;
                if (idx + by >= Fgs().Count) {
                    Fgs().Remove(style);
                    Bgs().Insert(0, style);
                    RefreshPanel();
                    return;
                }

                Fgs().Remove(style);
                Fgs().Insert(idx + by, style);
            } else {
                int indx = Bgs().IndexOf(style);
                if (indx + by < 0) {
                    Bgs().Remove(style);
                    Fgs().Add(style);
                    RefreshPanel();
                    return;
                }

                if (indx + by >= Bgs().Count)
                    return;
                Bgs().Remove(style);
                Bgs().Insert(indx + by, style);
            }

            SelectedStyleground += by;
            RefreshPanel();
        }
    }

    private UIButton SelectedButton() =>
        StylegroundButtons.Count > SelectedStyleground ? StylegroundButtons[SelectedStyleground] : null;

    public override string GetName() =>
        Dialog.Clean("SNOWBERRY_EDITOR_TOOL_STYLEGROUNDS");

    public override UIElement CreateActionBar() {
        UIElement bar = new();
        bar.AddRight(MapInfoTool.CreateScaleButton(), new(0, 4));
        return bar;
    }

    public override void Update(bool canClick) {
        for (int i = 0; i < StylegroundButtons.Count; i++) {
            UIButton item = StylegroundButtons[i];
            if (i == SelectedStyleground) {
                item.BG = item.HoveredBG = item.PressedBG = LeftSelectedBtnBg;
            } else if (Stylegrounds[item].IsVisible(Editor.SelectedRoom)) {
                item.BG = item.HoveredBG = item.PressedBG = Color.Lerp(BothSelectedBtnBg, Color.Black, 0.5f);
            } else {
                item.ResetBgColors();
            }
        }

        if (SelectedButton() != null) {
            var styleground = Stylegrounds[SelectedButton()];
            if (!IsFg(styleground) || Fgs().IndexOf(styleground) > 0 ) {
                MoveUp.ResetFgColors();
            } else {
                MoveUp.FG = MoveUp.HoveredFG = MoveUp.PressedFG = Color.DarkSlateGray;
            }

            if (IsFg(styleground) || Bgs().IndexOf(styleground) < Bgs().Count - 1) {
                MoveDown.ResetFgColors();
            } else {
                MoveDown.FG = MoveDown.HoveredFG = MoveDown.PressedFG = Color.DarkSlateGray;
            }
        } else {
            Delete.FG = Delete.HoveredFG = Delete.PressedFG = Color.DarkSlateGray;
            MoveUp.FG = MoveUp.HoveredFG = MoveUp.PressedFG = Color.DarkSlateGray;
            MoveDown.FG = MoveDown.HoveredFG = MoveDown.PressedFG = Color.DarkSlateGray;
        }
    }

    private void RefreshPanel() {
        // just regenerate the panel and set the scroll again
        var tempScroll = Editor.Instance.ToolPanel.NestedChildWithTag<UIScrollPane>("stylegrounds_list").Scroll;
        Editor.Instance.SwitchTool(Tools.IndexOf(this));
        Editor.Instance.ToolPanel.NestedChildWithTag<UIScrollPane>("stylegrounds_list").Scroll = tempScroll;
    }

    private bool IsFg(Styleground s) {
        return Fgs().Contains(s);
    }

    private List<Styleground> Fgs() {
        return Editor.Instance.Map.FGStylegrounds;
    }

    private List<Styleground> Bgs() {
        return Editor.Instance.Map.BGStylegrounds;
    }
}