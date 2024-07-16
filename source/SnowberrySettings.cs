﻿using System.Collections.Generic;
using Celeste.Mod;

namespace Snowberry;

public class SnowberrySettings : EverestModuleSettings {
    [SettingName("SNOWBERRY_SETTINGS_MIDDLE_CLICK_PAN")]
    [SettingSubText("SNOWBERRY_SETTINGS_MIDDLE_CLICK_PAN_SUB")]
    public bool MiddleClickPan { get; set; } = true;

    [SettingName("SNOWBERRY_SETTINGS_PAN_WRAPS_MOUSE")]
    [SettingSubText("SNOWBERRY_SETTINGS_PAN_WRAPS_MOUSE_SUB")]
    public bool PanWrapsMouse { get; set; } = false;

    [SettingName("SNOWBERRY_SETTINGS_FANCY_RENDER")]
    [SettingSubText("SNOWBERRY_SETTINGS_FANCY_RENDER_SUB")]
    public bool FancyRender { get; set; } = true;

    [SettingName("SNOWBERRY_SETTINGS_SG_PREVIEW")]
    [SettingSubText("SNOWBERRY_SETTINGS_SG_PREVIEW_SUB")]
    public bool StylegroundsPreview { get; set; } = true;

    [SettingName("SNOWBERRY_SETTINGS_AGGRESSIVE_SNAP")]
    [SettingSubText("SNOWBERRY_SETTINGS_AGGRESSIVE_SNAP_SUB")]
    public bool AggressiveSnap { get; set; } = false;

    [SettingName("SNOWBERRY_SETTINGS_SMALL_SCALE")]
    [SettingSubText("SNOWBERRY_SETTINGS_SMALL_SCALE_SUB")]
    public bool SmallScale { get; set; } = false;

    // saved but not displayed
    [SettingIgnore]
    public Dictionary<string, (bool show, bool record)> RecorderSettings { get; set; } = new();
}