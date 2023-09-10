using System.Globalization;
using System.Text.RegularExpressions;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.Editor;
using Snowberry.Editor.Tools;
using Snowberry.UI.Menus;
using WindController = Celeste.WindController;

namespace Snowberry.UI;

class UIRoomSelectionPanel : UIElement {
    public Color BG = Calc.HexToColor("202929") * 0.5f;

    public UIRoomSelectionPanel() {
        GrabsClick = GrabsScroll = true;
    }

    public override void Render(Vector2 position = default) {
        Draw.Rect(Bounds, BG);
        base.Render(position);
    }

    public void Refresh() {
        Clear();
        UIElement label;

        var offset = new Vector2(4, 3);
        if (Editor.Editor.SelectedRoom == null) {
            if (!RoomTool.PendingRoom.HasValue) {
                if (Editor.Editor.SelectedFillerIndex != -1) {
                    Add(label = new UILabel(Dialog.Get("SNOWBERRY_EDITOR_ROOM_FILL_SELECTED_TITLE").Substitute(Editor.Editor.SelectedFillerIndex)) {
                        FG = Color.DarkKhaki,
                        Underline = true
                    });
                    label.Position = Vector2.UnitX * (Width / 2 - label.Width / 2);

                    AddBelow(new UIButton(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_DELETE"), Fonts.Regular, 4, 4) {
                        FG = Color.Red,
                        HoveredFG = Color.Crimson,
                        PressedFG = Color.DarkRed,
                        OnPress = () => {
                            Editor.Editor.Instance.Map.Fillers.RemoveAt(Editor.Editor.SelectedFillerIndex);
                            Editor.Editor.SelectedFillerIndex = -1;
                            RoomTool.ScheduledRefresh = true;
                        }
                    }, new Vector2(4, 12));
                } else {
                    Add(label = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_NONE_TITLE")) {
                        FG = Color.DarkKhaki,
                        Underline = true
                    });
                    label.Position = Vector2.UnitX * (Width / 2 - label.Width / 2);
                }

                return;
            }

            Add(label = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_CREATE_TITLE")) {
                FG = Color.DarkKhaki,
                Underline = true
            });
            label.Position = Vector2.UnitX * (Width / 2 - label.Width / 2);

            string newName = "";
            UILabel newNameInvalid, newNameTaken;
            UIButton newRoom;

            AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_NAME"), newName, text => newName = text), offset);

            AddBelow(newRoom = new UIButton(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_CREATE_ROOM"), Fonts.Regular, 2, 2) {
                Position = new Vector2(4, 4),
            });
            Add(newNameInvalid = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_INVALID_NAME")) {
                Position = new Vector2(newRoom.Position.X + newRoom.Width + 5, newRoom.Position.Y + 3),
                FG = Color.Transparent
            });
            Add(newNameTaken = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_USED_NAME")) {
                Position = new Vector2(newRoom.Position.X + newRoom.Width + 5, newRoom.Position.Y + 3),
                FG = Color.Transparent
            });
            newRoom.OnPress = () => {
                newNameInvalid.FG = newNameTaken.FG = Color.Transparent;
                // validate room name
                if (newName.Length <= 0 || Regex.Match(newName, "[0-9a-zA-Z\\-_ ]+").Length != newName.Length)
                    newNameInvalid.FG = Color.Red;
                else if (Editor.Editor.Instance.Map.Rooms.Exists(it => it.Name.Equals(newName)))
                    newNameTaken.FG = Color.Red;
                else {
                    // add room
                    var b = RoomTool.PendingRoom.Value;
                    var newRoom = new Room(newName, new Rectangle(b.X / 8, b.Y / 8, b.Width / 8, b.Height / 8), Editor.Editor.Instance.Map);
                    Editor.Editor.Instance.Map.Rooms.Add(newRoom);
                    Editor.Editor.SelectedRoom = newRoom;
                    RoomTool.PendingRoom = null;
                    RoomTool.ScheduledRefresh = true;
                }
            };

            AddBelow(new UIButton(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_CREATE_FILLER"), Fonts.Regular, 2, 2) {
                Position = new Vector2(4, 4),
                OnPress = () => {
                    var b = RoomTool.PendingRoom.Value;
                    var newFiller = new Rectangle(b.X / 8, b.Y / 8, b.Width / 8, b.Height / 8);
                    Editor.Editor.Instance.Map.Fillers.Add(newFiller);
                    Editor.Editor.SelectedFillerIndex = Editor.Editor.Instance.Map.Fillers.Count - 1;
                    RoomTool.PendingRoom = null;
                    RoomTool.ScheduledRefresh = true;
                }
            });

            return;
        }

        int spacing = Fonts.Regular.LineHeight + 2;
        Room room = Editor.Editor.SelectedRoom;

        Add(label = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_SELECTED_TITLE")) {
            FG = Color.DarkKhaki,
            Underline = true
        });
        label.Position = Vector2.UnitX * (Width / 2 - label.Width / 2);

        string name = room.Name;
        UILabel nameInvalid, nameTaken;
        UIButton updateName;

        AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_NAME"), room.Name, text => name = text), offset);

        AddBelow(updateName = new UIButton(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_UPDATE_NAME"), Fonts.Regular, 2, 2) {
            Position = new Vector2(4, 4),
        });
        Add(nameInvalid = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_INVALID_NAME")) {
            Position = new Vector2(updateName.Position.X + updateName.Width + 5, updateName.Position.Y + 3),
            FG = Color.Transparent
        });
        Add(nameTaken = new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_USED_NAME")) {
            Position = new Vector2(updateName.Position.X + updateName.Width + 5, updateName.Position.Y + 3),
            FG = Color.Transparent
        });
        updateName.OnPress = () => {
            nameInvalid.FG = nameTaken.FG = Color.Transparent;
            // validate room name
            if (name.Length <= 0 || Regex.Match(name, "[0-9a-zA-Z\\-_ ]+").Length != name.Length)
                nameInvalid.FG = Color.Red;
            else if (room.Map.Rooms.Exists(it => it.Name.Equals(name)))
                nameTaken.FG = Color.Red;
            else
                room.Name = name;
        };

        AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_MUSIC")), new Vector2(12, 12));

        AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPT_MUSIC"), room.Music, text => room.Music = text), offset);
        AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPT_ALT_MUSIC"), room.AltMusic, text => room.AltMusic = text), offset);
        AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPT_AMBIENCE"), room.Ambience, text => room.Ambience = text), offset);

        AddBelow(UIPluginOptionList.LiteralValueOption<int>(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPT_MUSIC_PROG"), room.MusicProgress.ToString(), prog => room.MusicProgress = prog), offset);
        AddBelow(UIPluginOptionList.LiteralValueOption<int>(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPT_AMBIENCE_PROG"), room.AmbienceProgress.ToString(), prog => room.AmbienceProgress = prog), offset);

        Vector2 titleOffset = new Vector2(12, 8);
        AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_MUSIC_LAYERS")), titleOffset);
        UIElement layers = new();
        for (int i = 0; i < 4; i++) {
            var c = i;
            layers.AddRight(UIPluginOptionList.BoolOption((c + 1).ToString(), room.MusicLayers[c], val => room.MusicLayers[c] = val), new(6, 0));
        }
        layers.CalculateBounds();
        AddBelow(layers, offset - new Vector2(6, 0)); // the first checkbox is incorrectly offset right

        AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_CAMERA_OFFSET")), titleOffset);
        UIElement coords = new();
        coords.AddRight(UIPluginOptionList.LiteralValueOption<float>("x", room.CameraOffset.X.ToString(CultureInfo.InvariantCulture), val => room.CameraOffset.X = val, width: 40), new(4, 0));
        coords.AddRight(UIPluginOptionList.LiteralValueOption<float>("y", room.CameraOffset.Y.ToString(CultureInfo.InvariantCulture), val => room.CameraOffset.Y = val, width: 40), new(15, 0));
        coords.CalculateBounds();
        AddBelow(coords);

        AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_OTHER")), titleOffset);
        AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_DARK"), room.Dark, val => room.Dark = val ), offset);
        AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_UNDERWATER"), room.Underwater, val => room.Underwater = val), offset);
        AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_SPACE"), room.Space, val => room.Space = val), offset);
        AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_DDS"), room.DisableDownTransition, val => room.DisableDownTransition = val), offset);
        AddBelow(UIPluginOptionList.DropdownOption<WindController.Patterns>(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_OPTS_WIND"), room.WindPattern, it => room.WindPattern = it), offset);

        AddBelow(new UIButton(Dialog.Clean("SNOWBERRY_EDITOR_ROOM_DELETE"), Fonts.Regular, 4, 4) {
            FG = Color.Red,
            HoveredFG = Color.Crimson,
            PressedFG = Color.DarkRed,
            OnPress = () => {
                Editor.Editor.Instance.Map.Rooms.Remove(room);
                Editor.Editor.SelectedRoom = null;
                RoomTool.ScheduledRefresh = true;
            }
        }, new Vector2(4, 12));
    }
}