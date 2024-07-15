﻿using System.IO;
using System.Linq;
using Celeste;
using Monocle;
using Snowberry.Editor;
using Snowberry.UI;

namespace Snowberry;

internal class Commands {

    [Command("editor", "opens the snowberry level editor")]
    internal static void EditorCommand(string mapSid = null) {
        if (mapSid != null) {
            var mapData = AreaData.Get(mapSid)?.Mode[0]?.MapData;
            if (mapData != null)
                Editor.Editor.Open(mapData);
            else {
                Engine.Commands.Log($"found no map with SID {mapSid}! (or it failed to load)");
                var similar = AreaData.Areas.Where(x => x.SID.StartsWith(mapSid)).ToList();
                if (similar.Count > 0) {
                    var look = similar.Skip(1).Aggregate(similar.First().SID, (s, data) => $"{s}, {data.SID}");
                    Engine.Commands.Log($"try {look}?");
                }
            }
        }

        if (Engine.Scene is Level l)
            Editor.Editor.Open(l.Session.MapData);
        else
            MainMenu.OpenMainMenu(fast: true);
    }

    [Command("editor_new", "opens the snowberry level editor on an empty map")]
    internal static void NewMapCommand() {
        Editor.Editor.OpenNew();
    }

    [Command("editor_surgery", "opens the snowberry surgery screen for low-level map manipulation")]
    internal static void SurgeryCommand(string mapPath) {
        if (mapPath == null) {
            if (Engine.Scene is Level l) {
                string path = l.Session.MapData.Filepath;
                if (string.IsNullOrEmpty(path)) {
                    Engine.Commands.Log("could not find the map file for the current map,");
                    Engine.Commands.Log("provide a map path, starting from & including Mods/");
                    return;
                }
                mapPath = path;
            } else {
                Engine.Commands.Log("provide a map path, starting from & including Mods/");
                return;
            }
        }

        var file = Files.GetRealPath(mapPath) ?? mapPath;
        if (File.Exists(file))
            Engine.Scene = new Surgery.Surgery(mapPath, BinaryPacker.FromBinary(mapPath));
        else
            Engine.Commands.Log($"could not find map file {mapPath}");
    }

    [Command("editor_mixer", "opens the snowberry audio mixer screen for testing audio")]
    internal static void UIMixerCommand() {
        Engine.Scene = new Mixer.Mixer();
    }

    [Command("editor_ui_bounds", "toggles displaying the bounds of all snowberry UI elements")]
    internal static void UIBoundsCommand() {
        UIScene.DebugShowUIBounds = !UIScene.DebugShowUIBounds;
    }

    [Command("editor_ui_example", "opens a screen displaying examples of various UI elements")]
    internal static void UIExampleCommand() {
        Engine.Scene = new Example();
    }
}