using System;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.UI;
using Snowberry.UI.Menus;
using static Snowberry.Editor.Triggers.Plugin_ChangeInventoryTrigger;

namespace Snowberry.Editor.Tools;

public class MapInfoTool : Tool {

    public override string GetName() => Dialog.Clean("SNOWBERRY_EDITOR_TOOL_MAPINFO");

    public override UIElement CreatePanel(int height){
        var ret = new UIElement{
            Width = 220,
            Height = height,
            GrabsClick = true,
            GrabsScroll = true,
            Background = Calc.HexToColor("202929") * 0.5f
        };

        Map map = Editor.Instance.Map;

        Vector2 titleOffset = new(12, 3), optionOffset = new(4, 3);

        ret.AddBelow(new UILabel($"editing {map.Name}"){
            Underline = true
        }, titleOffset);

        // TODO: initialize with defaults and put them somewhere proper
        // overworld
        ret.AddBelow(UIPluginOptionList.StringOption("icon", map.Meta.Icon ?? "", i => map.Meta.Icon = i), optionOffset);
        // intro
        ret.AddBelow(UIPluginOptionList.StringOption("wipe", map.Meta.Wipe ?? "Celeste.AngledWipe", i => map.Meta.Wipe = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.DropdownOption("intro type", map.Meta.IntroType ?? Player.IntroTypes.None, i => map.Meta.IntroType = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.DropdownOption("core mode", map.Meta.CoreMode ?? Session.CoreModes.None, i => map.Meta.CoreMode = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption("dreaming", map.Meta.Dreaming ?? false, i => map.Meta.Dreaming = i), optionOffset);
        // ambient visuals
        ret.AddBelow(UIPluginOptionList.LiteralValueOption<float>("darkness alpha", map.Meta.DarknessAlpha?.ToString() ?? "0.05", i => map.Meta.DarknessAlpha = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.LiteralValueOption<float>("bloom base", map.Meta.BloomBase?.ToString() ?? "0", i => map.Meta.BloomBase = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.LiteralValueOption<float>("bloom strength", map.Meta.BloomStrength?.ToString() ?? "1", i => map.Meta.BloomStrength = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption("color grade", map.Meta.ColorGrade ?? "", i => map.Meta.ColorGrade = i), optionOffset);
        // music
        ret.AddBelow(UIPluginOptionList.StringOption("cassette song", map.Meta.CassetteSong ?? "event:/music/cassette/01_forsaken_city", i => map.Meta.CassetteSong = i), optionOffset);
        // reskins
        ret.AddBelow(UIPluginOptionList.StringOption("sprites xml", map.Meta.Sprites ?? "", i => map.Meta.Sprites = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption("portraits xml", map.Meta.Portraits ?? "", i => map.Meta.Portraits = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption("FG tiles xml", map.Meta.ForegroundTiles ?? "", i => map.Meta.ForegroundTiles = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption("BG tiles xml", map.Meta.BackgroundTiles ?? "", i => map.Meta.BackgroundTiles = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption("anim. tiles xml", map.Meta.AnimatedTiles ?? "", i => map.Meta.AnimatedTiles = i), optionOffset);

        // mode meta
        ret.AddBelow(UIPluginOptionList.BoolOption("override a-side meta", map.Meta.OverrideASideMeta ?? false, i => map.Meta.OverrideASideMeta = i), optionOffset);
        ret.AddBelow(new UILabel("current side :"), new(12, 6));
        ret.AddBelow(UIPluginOptionList.StringOption("poem dialog key", map.Meta.Modes[0].PoemID ?? "", i => map.Meta.Modes[0].PoemID = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.StringOption("starting room", map.Meta.Modes[0].StartLevel ?? "", i => map.Meta.Modes[0].StartLevel = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.DropdownOption("starting inventory", Enum.TryParse(map.Meta.Modes[0].Inventory, false, out InventoryType inv) ? inv : InventoryType.Default, i => map.Meta.Modes[0].Inventory = i.ToString()), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption("end level on heart", map.Meta.Modes[0].HeartIsEnd ?? false, i => map.Meta.Modes[0].HeartIsEnd = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption("seeker slowdown", map.Meta.Modes[0].SeekerSlowdown ?? true, i => map.Meta.Modes[0].SeekerSlowdown = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption("carry theo in boosters", map.Meta.Modes[0].TheoInBubble ?? false, i => map.Meta.Modes[0].TheoInBubble = i), optionOffset);
        ret.AddBelow(UIPluginOptionList.BoolOption("ignore music layers", map.Meta.Modes[0].IgnoreLevelAudioLayerData ?? false, i => map.Meta.Modes[0].IgnoreLevelAudioLayerData = i), optionOffset);

        return ret;
    }

    public override void Update(bool canClick){}
}