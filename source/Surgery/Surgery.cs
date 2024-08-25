using System.Collections.Generic;
using System.IO;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.UI;
using Snowberry.UI.Controls;
using Snowberry.UI.Layout;

namespace Snowberry.Surgery;

using Element = BinaryPacker.Element;

public class Surgery : UIScene {

    private UIElement TopBar;
    private UIScrollPane Rest;
    private string path;
    private readonly Element elem;

    public Surgery(string path, Element elem) {
        if (path.StartsWith(Everest.PathGame))
            path = path[Everest.PathGame.Length..].TrimStart('/', '\\');

        this.path = path;
        this.elem = elem;
    }

    public override void Begin() {
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
        Audio.Stop(Audio.CurrentMusicEventInstance);
        base.Begin();
    }

    protected override void BeginContent() {
        SurgeryUi();
    }

    internal void SurgeryUi() {
        TopBar = new() {
            Background = Color.DarkRed,
            Width = UI.Width,
            Height = 40
        };

        TopBar.AddRight(new UILabel("snowberry", Fonts.Regular, 2), new(8, 8));
        TopBar.AddRight(new UILabel(Dialog.Clean("SNOWBERRY_SURGERY_TITLE"), Fonts.Bold, 2) {
            Underline = true,
            FG = Color.Red
        }, new(8, 8));
        TopBar.AddRight(new UILabel($"{Dialog.Clean("SNOWBERRY_SURGERY_PATHING")} {path}", Fonts.Regular) {
            FG = Color.Gray
        }, new(8, 20));

        UITextField mapName = new UITextField(Fonts.Regular, 400, Path.GetFileName(path));
        TopBar.AddRight(new UIButton(ActionbarAtlas.GetSubtexture(16, 0, 16, 16), 3, 3) {
            OnPress = () => BinaryExporter.ExportToFile(elem, Path.Combine(Everest.Loader.PathMods, mapName.Value + ".bin"))
        }, new(40, 8));
        TopBar.AddRight(mapName, new(8, 14));

        TopBar.AddRight(new UIButton(ActionbarAtlas.GetSubtexture(32, 0, 16, 16), 3, 3) {
            OnPress = () => Engine.Scene = new OverworldLoader(Overworld.StartMode.MainMenu)
        }, new(8));

        UI.Add(TopBar);

        Rest = new() {
            Width = UI.Width,
            Height = UI.Height - 40,
            Position = new(0, 40),
            TopPadding = 10
        };

        Rest.AddBelow(Render(elem, null), new(10));

        UI.Add(Rest);
    }

    protected override void OnScreenResized() {
        base.OnScreenResized();

        TopBar.Width = UI.Width;
        Rest.Width = UI.Width;
        Rest.Height = UI.Height - 40;
    }

    internal UIElement Render(Element e, Element parent) {
        UILabel header = string.IsNullOrEmpty(e.Name)
            ? new UILabel(e.Name is null ? "(null)" : "(blank)") {
                FG = Color.Gray
            }
            : new UILabel(e.Name);

        UITree ret = new(header);

        if(parent != null){
            ret.AddRight(new UIButton(Dialog.Clean("SNOWBERRY_SURGERY_DELETE"), Fonts.Regular) {
                OnPress = () => {
                    parent.Children?.Remove(e);
                    ret.RemoveSelf();
                    (ret.Parent as UITree)?.LayoutUp();
                },
                FG = Color.Red,
                HoveredFG = Color.Crimson,
                PressedFG = Color.DarkRed
            }, new(3, -1));
        }

        if (e.Attributes != null)
            foreach (KeyValuePair<string, object> kvp in e.Attributes) {
                UIElement attr = new();
                attr.Add(new UILabel(kvp.Key));
                attr.AddRight(new UILabel("="), new(4, 0));
                attr.AddRight(new UILabel($"({(kvp.Value?.GetType()?.Name ?? "null")})") {
                    FG = Color.Gray
                }, new(4, 0));
                var text = kvp.Value?.ToString() ?? "null";
                attr.AddRight(new UILabel(text.Contains('\n') ? "..." : text), new(4, 0));
                attr.CalculateBounds();

                ret.AddBelow(attr, new(8, 2));
            }

        if (e.Children != null)
            foreach (Element child in e.Children)
                ret.AddBelow(Render(child, e), new(20, 4));

        ret.Layout();
        return ret;
    }
}