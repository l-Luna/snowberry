using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Xml;

namespace Snowberry; 

public static class Fonts {
    public static Font Regular { get; private set; }
    public static Font Bold { get; private set; }
    public static Font Pico8 { get; private set; }

    internal static void Load() {
        Regular = LoadFont("regular");
        Bold = LoadFont("bold");
        Pico8 = LoadFont("pico8");
    }

    private static Font LoadFont(string name) {
        ModAsset data = Everest.Content.Get($"Snowberry:/Content/fonts/{name}Data");

        Texture2D texture = GFX.Gui[$"Snowberry/fonts/{name}"].Texture.Texture_Safe;

        XmlDocument xml = new XmlDocument();
        xml.Load(data.Stream);
        XmlElement root = xml.DocumentElement;

        List<char> characters = new List<char>();
        List<Rectangle> bounds = new List<Rectangle>();
        List<Vector2> offsets = new List<Vector2>();
        int lineHeight = int.Parse(root["common"].GetAttribute("lineHeight"));

        foreach (XmlElement node in root["chars"].ChildNodes) {
            char c = (char)int.Parse(node.GetAttribute("id"));
            int x = int.Parse(node.GetAttribute("x"));
            int y = int.Parse(node.GetAttribute("y"));
            int w = int.Parse(node.GetAttribute("width"));
            int h = int.Parse(node.GetAttribute("height"));
            //int ox = int.Parse(node.GetAttribute("xoffset")); don't care
            int oy = int.Parse(node.GetAttribute("yoffset"));

            characters.Add(c);
            bounds.Add(new Rectangle(x, y, w, h));
            offsets.Add(new Vector2(0, -oy));
        }

        return new Font(texture, characters, bounds, offsets, lineHeight);
    }
}