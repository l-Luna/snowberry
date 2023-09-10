using System.IO;
using System.Linq;
using Celeste;
using Monocle;
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

        Editor.Editor.Open(Engine.Scene is Level level ? level.Session.MapData : null);
    }

    [Command("editor_new", "opens the snowberry level editor on an empty map")]
    internal static void NewMapCommand() {
        Editor.Editor.OpenNew();
    }

    [Command("editor_surgery", "opens the snowberry surgery screen for low-level map manipulation")]
    internal static void SurgeryCommand(string mapPath) {
        var file = Util.GetRealPath(mapPath);
        if (File.Exists(file))
            Engine.Scene = new Surgery.Surgery(mapPath, BinaryPacker.FromBinary(mapPath));
        else
            Engine.Commands.Log($"could not find map file {mapPath ?? "null"}");
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