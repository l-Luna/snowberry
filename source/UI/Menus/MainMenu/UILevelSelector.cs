using System.Linq;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.UI.Menus.MainMenu;

public class UILevelSelector : UIElement {
    public float LevelRibbonAnim;
    private int lvlCount;

    private UISearchBar<UILevelRibbon> searchBar;
    private UILevelRibbon[] levels;

    public void Reload() {
        Clear();

        LevelRibbonAnim = 0f;

        UIScrollPane levelScrollPane = new UIScrollPane {
            Height = Parent.Height - 30,
            Position = new Vector2(-16, 22),
            BG = Color.Transparent,
            Background = Color.Transparent,
            ShowScrollBar = false
        };

        // TODO: catch other generated levels
        var filteredAreas = AreaData.Areas.Where(x => x.SID != "Snowberry/Playtest").ToList();

        levels = new UILevelRibbon[lvlCount = filteredAreas.Count];
        int y = 0;
        Width = 0;
        for (int i = 0; i < lvlCount; i++) {
            AreaData area = filteredAreas[i];
            UILevelRibbon lvl;
            levelScrollPane.Add(lvl = new UILevelRibbon(this, area, i) {
                Position = new Vector2(-10, y),
                FG = area.TitleTextColor,
                BG = area.TitleBaseColor,
                BGAccent = area.TitleAccentColor,
            });

            if (lvl.W > Width)
                Width = lvl.W;
            y += 13;

            levels[i] = lvl;
        }
        Add(levelScrollPane);
        levelScrollPane.Width = Width;

        static bool lvlMatcher(UILevelRibbon entry, string term)
            => entry.Text.ToLower().Contains(term.ToLower());

        static bool lvlMatcherByMod(UILevelRibbon entry, string term)
            => entry.Name.ToLower().Contains(term.ToLower());

        string infonone = Dialog.Clean("SNOWBERRY_MAINMENU_LOADSEARCHBAR_NONE");
        string infoone = Dialog.Clean("SNOWBERRY_MAINMENU_LOADSEARCHBAR_ONE");
        string infomore = Dialog.Clean("SNOWBERRY_MAINMENU_LOADSEARCHBAR_MORE");
        Add(searchBar = new UISearchBar<UILevelRibbon>(Width / 2, lvlMatcher) {
            Position = Vector2.UnitY * 8,
            Entries = levels,
            InfoText = Dialog.Clean("SNOWBERRY_MAINMENU_LOADSEARCH"),
            SearchInfo = count => {
                return count switch {
                    0 => $"{infonone}",
                    1 => $"{infoone}",
                    _ => $"{count} {infomore}"
                };
            },
            OnInputChange = s => {
                if (levels != null)
                    levels[0].Position.Y = 0;
                LevelRibbonAnim = lvlCount;
                levelScrollPane.Scroll = 0;
            }
        });
        searchBar.AddSpecialMatcher('@', lvlMatcherByMod, Calc.HexToColor("1b6dcc"));
    }

    public override void Update(Vector2 position = default) {
        base.Update(position);

        LevelRibbonAnim = Calc.Approach(LevelRibbonAnim, lvlCount, Engine.DeltaTime * 60f);

        if (levels != null) {
            int y = (int)levels[0].Position.Y;
            foreach (UILevelRibbon lvl in levels) {
                lvl.Visible = searchBar.Found == null || searchBar.Found.Contains(lvl);
                if (lvl.Visible) {
                    lvl.Position.Y = y;
                    y += 13 + lvl.H;
                }
            }
        }
    }
}