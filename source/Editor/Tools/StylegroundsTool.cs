using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;
using Snowberry.UI;
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
        foreach (var styleground in Editor.Instance.Map.FGStylegrounds) {
            int copy = i;
            UIButton element = new UIButton(styleground.Title(), Fonts.Regular, 4, 2) {
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
            UIButton element = new UIButton(styleground.Title(), Fonts.Regular, 4, 2) {
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
        optionsPanel.AddRight(Add = new UIButton("+ \uF036", Fonts.Regular, 4, 4) {
            // add new styleground
            OnPress = () => {
                Editor.Instance.ToolPanel.Add(new UIDropdown(Fonts.Regular, PluginInfo.Stylegrounds.Select(k => new UIDropdown.DropdownEntry(k.Key, () => {
                    var newSg = k.Value.Instantiate<Styleground>();
                    var selected = SelectedButton();
                    if (selected == null) {
                        Bgs().Add(newSg);
                    } else {
                        var style = Stylegrounds[selected];
                        if (IsFg(style)) {
                            Fgs().Insert(SelectedStyleground, newSg);
                        } else {
                            Bgs().Insert(SelectedStyleground - Fgs().Count, newSg);
                        }
                    }

                    RefreshPanel();
                }) {
                    BG = BothSelectedBtnBg,
                    HoveredBG = Color.Lerp(BothSelectedBtnBg, Color.Black, 0.25f),
                    PressedBG = Color.Lerp(BothSelectedBtnBg, Color.Black, 0.5f)
                }).ToArray()) {
                    Position = (Add.GetBoundsPos() + Vector2.UnitY * (Add.Height + 2)) - Editor.Instance.ToolPanel.GetBoundsPos()
                });
            }
        }, new Vector2(4));

        optionsPanel.AddRight(Delete = new UIButton("-", Fonts.Regular, 4, 4) {
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

        optionsPanel.AddRight(MoveUp = new UIButton("↑", Fonts.Regular, 4, 4) {
            OnPress = () => {
                MoveStyleground(-1);
            }
        }, new Vector2(4));

        optionsPanel.AddRight(MoveDown = new UIButton("↓", Fonts.Regular, 4, 4) {
            OnPress = () => {
                MoveStyleground(1);
            }
        }, new Vector2(4));

        optionsPanel.Height = optionsPanel.Children.Select(k => k.Height).Max() + 8;
        panel.AddBelow(optionsPanel);

        UIElement stylegroundInfo = new() {
            Tag = "stylegrounds_info",
            Width = 175,
            Position = new Vector2(5, 0)
        };

        panel.AddBelow(stylegroundInfo);
        AddStylegroundInfo(stylegroundInfo);
        return panel;
    }

    private void AddStylegroundInfo(UIElement panel) {
        panel.Clear();
        var selected = SelectedButton();
        // might not have any stylegrounds
        if(selected != null && Stylegrounds.TryGetValue(selected, out var styleground)){
            var offset = Vector2.UnitY * 4;
            panel.Add(UIPluginOptionList.StringOption("Only In", styleground.OnlyIn, s => styleground.OnlyIn = s));
            panel.AddBelow(UIPluginOptionList.StringOption("Not In", styleground.ExcludeFrom, s => styleground.ExcludeFrom = s), offset);
            panel.AddBelow(UIPluginOptionList.StringOption("Flag", styleground.Flag, s => styleground.Flag = s), offset);
            panel.AddBelow(UIPluginOptionList.StringOption("Not Flag", styleground.NotFlag, s => styleground.NotFlag = s), offset);
            panel.AddBelow(UIPluginOptionList.StringOption("Force Flag", styleground.ForceFlag, s => styleground.ForceFlag = s), offset);
            panel.AddBelow(UIPluginOptionList.ColorOption("Colour", styleground.Color, c => styleground.Color = c), offset);
            panel.AddBelow(UIPluginOptionList.StringOption("Tags", styleground.Tags, t => styleground.Tags = t), offset);

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

    private UIButton SelectedButton() {
        return StylegroundButtons.Count > SelectedStyleground ? StylegroundButtons[SelectedStyleground] : null;
    }

    public override string GetName() {
        return Dialog.Clean("SNOWBERRY_EDITOR_TOOL_STYLEGROUNDS");
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