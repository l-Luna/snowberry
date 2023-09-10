using System.Collections.Generic;
using System.IO;
using Celeste;
using Microsoft.Xna.Framework;
using Snowberry.UI;

namespace Snowberry.Surgery;

using Element = BinaryPacker.Element;

public class Surgery : UIScene {

    private string path;
    private Element elem;

    public Surgery(string path, Element elem) {
        this.path = path;
        this.elem = elem;
    }

    protected override void BeginContent() {
        SurgeryUi();
    }

    internal void SurgeryUi() {
        UIElement topBar = new() {
            Background = Color.DarkRed,
            Width = UI.Width,
            Height = 40
        };

        topBar.AddRight(new UILabel("snowberry", Fonts.Regular, 2), new(8, 8));
        topBar.AddRight(new UILabel("surgery", Fonts.Bold, 2) {
            Underline = true,
            FG = Color.Red
        }, new(8, 8));
        topBar.AddRight(new UILabel($"on {path}", Fonts.Regular) {
            FG = Color.Gray
        }, new(8, 20));

        UITextField mapName = new UITextField(Fonts.Regular, 400, Path.GetFileName(path));
        topBar.AddRight(new UIButton(ActionbarAtlas.GetSubtexture(16, 0, 16, 16), 3, 3) {
            OnPress = () => BinaryExporter.Export(elem, mapName.Value + ".bin")
        }, new(40, 8));
        topBar.AddRight(mapName, new(8, 14));

        UI.Add(topBar);

        UIScrollPane rest = new() {
            Width = UI.Width,
            Height = UI.Height - 40,
            Position = new(0, 40),
            TopPadding = 10
        };

        rest.AddBelow(Render(elem, null), new(10));

        UI.Add(rest);
    }

    internal UIElement Render(Element e, Element parent) {
        UIElement ret = new();

        ret.Add(string.IsNullOrEmpty(e.Name)
            ? new UILabel(e.Name is null ? "(null)" : "(blank)") {
                FG = Color.Gray
            }
            : new UILabel(e.Name));

        if(parent != null){
            ret.AddRight(new UIButton("x", Fonts.Regular) {
                OnPress = () => {
                    parent.Children?.Remove(e);
                    ret.RemoveSelf();
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
                var text = (kvp.Value ?? "null").ToString();
                attr.AddRight(new UILabel(text.Contains("\n") ? "..." : text), new(4, 0));
                attr.CalculateBounds();

                ret.AddBelow(attr, new(8, 2));
            }

        if (e.Children != null)
            foreach (Element child in e.Children)
                ret.AddBelow(Render(child, e), new(20, 4));

        ret.CalculateBounds();
        return ret;
    }
}