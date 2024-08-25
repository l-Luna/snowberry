using System;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Snowberry.UI;
using Snowberry.UI.Controls;
using Snowberry.UI.Layout;
using Snowberry.UI.Menus;
using static Snowberry.Editor.Triggers.Plugin_ChangeInventoryTrigger;

namespace Snowberry.Editor.Tools;

public class MapInfoTool : Tool {

    public override string GetName() => Dialog.Clean("SNOWBERRY_EDITOR_TOOL_MAPINFO");

    public override UIElement CreatePanel(int height){
        var ret = new UIScrollPane {
            Width = 220,
            Height = height,
            GrabsClick = true,
            GrabsScroll = true,
            Background = Calc.HexToColor("202929") * 0.5f
        };

        Map map = Editor.Instance.Map;

        Vector2 optionOffset = new(4, 3);

        UIRibbon ribbon = new UIRibbon(map.Id.Name(), leftEdge: true, rightEdge: true) {
            FG = Calc.HexToColor(map.Meta.TitleTextColor ?? ""),
            BG = Calc.HexToColor(map.Meta.TitleBaseColor ?? ""),
            BGAccent = Calc.HexToColor(map.Meta.TitleAccentColor ?? "")
        };
        ribbon.Position.X = (ret.Width - ribbon.Width) / 2;
        ret.AddBelow(ribbon);

        // TODO: initialize with defaults and put them somewhere proper
        // overworld
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_ICON"), map.Meta.Icon ?? "", i => map.Meta.Icon = i), optionOffset);
        // intro
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_WIPE"), map.Meta.Wipe ?? "Celeste.AngledWipe", i => map.Meta.Wipe = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.DropdownOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_INTRO_TYPE"), map.Meta.IntroType ?? Player.IntroTypes.None, i => map.Meta.IntroType = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.DropdownOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_CORE_MODE"), map.Meta.CoreMode ?? Session.CoreModes.None, i => map.Meta.CoreMode = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_DREAMING"), map.Meta.Dreaming ?? false, i => map.Meta.Dreaming = i), optionOffset);
        // ambient visuals
        ret.AddBelow(UIPluginOptionList.LiteralValueOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_DARKNESS_ALPHA"), map.Meta.DarknessAlpha ?? 0.05f, i => map.Meta.DarknessAlpha = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.LiteralValueOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_BLOOM_BASE"), map.Meta.BloomBase ?? 0, i => map.Meta.BloomBase = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.LiteralValueOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_BLOOM_STRENGTH"), map.Meta.BloomStrength ?? 1, i => map.Meta.BloomStrength = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_COLORGRADE"), map.Meta.ColorGrade ?? "", i => map.Meta.ColorGrade = i), optionOffset);
        // music
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_CASSETTE_SONG"), map.Meta.CassetteSong ?? "event:/music/cassette/01_forsaken_city", i => map.Meta.CassetteSong = i), optionOffset);
        // reskins
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_SPRITES_XML"), map.Meta.Sprites ?? "", i => map.Meta.Sprites = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_PORTRAITS_XML"), map.Meta.Portraits ?? "", i => map.Meta.Portraits = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_FGTILES_XML"), map.Meta.ForegroundTiles ?? "", i => map.Meta.ForegroundTiles = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_BGTILES_XML"), map.Meta.BackgroundTiles ?? "", i => map.Meta.BackgroundTiles = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_ANIMTILES_XML"), map.Meta.AnimatedTiles ?? "", i => map.Meta.AnimatedTiles = i), optionOffset);

        // colours
        ret.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_MI_TITLE_COLOURS")), new(12, 6));
        ret.AddBelow(UIPluginOptionList.ColorOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_TITLE_TEXT_COLOUR"), Calc.HexToColor(map.Meta.TitleTextColor ?? ""), c => {
            map.Meta.TitleTextColor = c.IntoRgbString();
            ribbon.FG = c;
        }), optionOffset);
        ret.AddBelow(UIPluginOptionList.ColorOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_TITLE_BASE_COLOUR"), Calc.HexToColor(map.Meta.TitleBaseColor ?? ""), c => {
            map.Meta.TitleBaseColor = c.IntoRgbString();
            ribbon.BG = c;
        }), optionOffset);
        ret.AddBelow(UIPluginOptionList.ColorOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_TITLE_ACCENT_COLOUR"), Calc.HexToColor(map.Meta.TitleAccentColor ?? ""), c => {
            map.Meta.TitleAccentColor = c.IntoRgbString();
            ribbon.BGAccent = c;
        }), optionOffset);

        // mode meta
        ret.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_EDITOR_MI_CURRENT_SIDE")), new(12, 6));
        ret.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_OVERRIDE_ASIDE_META"), map.Meta.OverrideASideMeta ?? false, i => map.Meta.OverrideASideMeta = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_POEM_DIALOG_KEY"), map.Meta.Modes[0].PoemID ?? "", i => map.Meta.Modes[0].PoemID = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_START_ROOM"), map.Meta.Modes[0].StartLevel ?? "", i => map.Meta.Modes[0].StartLevel = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.DropdownOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_START_INVENTORY"), Enum.TryParse(map.Meta.Modes[0].Inventory, false, out InventoryType inv) ? inv : InventoryType.Default, i => map.Meta.Modes[0].Inventory = i.ToString()), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_END_LEVEL_ON_HEART"), map.Meta.Modes[0].HeartIsEnd ?? false, i => map.Meta.Modes[0].HeartIsEnd = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_SEEKER_SLOWDOWN"), map.Meta.Modes[0].SeekerSlowdown ?? true, i => map.Meta.Modes[0].SeekerSlowdown = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_THEO_BOOSTERS"), map.Meta.Modes[0].TheoInBubble ?? false, i => map.Meta.Modes[0].TheoInBubble = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption(Dialog.Clean("SNOWBERRY_EDITOR_MI_IGNORE_MUSIC_LAYERS"), map.Meta.Modes[0].IgnoreLevelAudioLayerData ?? false, i => map.Meta.Modes[0].IgnoreLevelAudioLayerData = i), optionOffset);

        return ret;
    }

    public override UIElement CreateActionBar() {
        UIElement bar = new();
        bar.AddRight(CreateScaleButton(), new(0, 4));

        bar.AddRight(CreateToggleButtonNoKb(64, 16, Keys.L, "STYLEGROUNDS", () => Snowberry.Settings.StylegroundsPreview,
            b => {
                Snowberry.Settings.StylegroundsPreview = b;
                Snowberry.Instance.SaveSettings();
            }), new(5, 4));
        bar.AddRight(CreateToggleButtonNoKb(64, 0, Keys.F, "FANCY_RENDERING", () => Snowberry.Settings.FancyRender,
            b => {
                Snowberry.Settings.FancyRender = b;
                Snowberry.Instance.SaveSettings();
            }), new(5, 4));
        return bar;
    }

    public override void Update(bool canClick){}

    public static UIKeyboundButton CreateScaleButton() =>
        new(UIScene.ActionbarAtlas.GetSubtexture(0, 80, 16, 16), 3, 3) {
            OnPress = () => Editor.Instance.Camera.Zoom = 6,
            ButtonTooltip = Dialog.Clean("SNOWBERRY_EDITOR_MI_ONE_TO_ONE_TT")
        };

    private static UIButton CreateToggleButtonNoKb(int icoX, int icoY, Keys toggleBind, string tooltipKey, Func<bool> value, Action<bool> onPress) {
        MTexture active = UIScene.ActionbarAtlas.GetSubtexture(icoX, icoY, 16, 16);
        MTexture inactive = UIScene.ActionbarAtlas.GetSubtexture(icoX + 16, icoY, 16, 16);
        UIKeyboundButton button = null; // to allow referring to it in OnPress
        button = new UIKeyboundButton(value() ? active : inactive, 3, 3) {
            OnPress = () => {
                onPress(!value());
                button.SetIcon(value() ? active : inactive);
            },
            OnKbPress = () => {
                // Editor actually handles these binds for us
                // since we update before Editor, we'll always lag behind by one switch
                button.SetIcon(value() ? inactive : active);
            },
            Ctrl = true,
            Key = toggleBind,
            ButtonTooltip = Dialog.Clean($"SNOWBERRY_EDITOR_MI_{tooltipKey}_TT")
        };
        return button;
    }
}