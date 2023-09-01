using Microsoft.Xna.Framework;
using Snowberry.Editor.UI.Menus;
using System.Collections.Generic;

namespace Snowberry.Editor.UI;

public class UISelectionPane : UIScrollPane{

    public UISelectionPane(){
        GrabsClick = true;
    }

    public void Display(List<Selection> selection){
        if(selection != null){
            Clear();
            int y = 0;
            foreach (Selection s in selection) {
                UIElement entry = AddEntry(s);
                entry.Position.Y = y;
                y += entry.Height + 8;
            }
        }
    }

    private UIElement AddEntry(Selection s){
        UIRibbon name = new UIRibbon(s.Name(), 8, 8, true, false) {
            BG = Util.Colors.DarkGray,
            BGAccent = s.Accent()
        };
        name.Position.X += Width - name.Width;
        UIElement entry = name;

        if(s is EntitySelection es) {
            UILabel id = new UILabel($"#{es.Entity.EntityID}") {
                FG = Util.Colors.White * 0.5f
            };
            id.Position.X = name.Position.X - id.Width - 4;

            UIPluginOptionList options = new UIPluginOptionList(es.Entity) {
                Position = new Vector2(3, name.Height + 3)
            };

            entry = Regroup(id, name, options);
        }
        Add(entry);
        return entry;
    }
}