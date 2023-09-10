using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Snowberry.UI; 

public class UISearchBar<T> : UITextField {
    public T[] Entries;
    public T[] Found { get; private set; }
    private Color[] highlighting;

    public string InfoText = "...";
    private string searchInfo;
    public Func<int, string> SearchInfo;

    public delegate bool TermMatcher(T entry, string term);

    private readonly TermMatcher termMatcher;
    private readonly Dictionary<char, Tuple<TermMatcher, Color, Regex>> specialChars = new();

    public UISearchBar(int width, TermMatcher termMatcher)
        : base(Fonts.Regular, width) {
        Line = Color.Transparent;
        this.termMatcher = termMatcher;
    }

    protected override void DrawText(Vector2 position) {
        if (highlighting != null && Value.Length > 0) {
            Font.Draw(Value, position, Vector2.One, Vector2.Zero, highlighting);
            if (searchInfo != null)
                Font.Draw(searchInfo, position + Vector2.UnitX * (ValueWidth + 10), Vector2.One, Util.Colors.White * 0.5f);
        } else
            Font.Draw(InfoText, position, Vector2.One, Util.Colors.White * 0.65f);
    }

    public void AddSpecialMatcher(char mode, TermMatcher specialMatcher, Color displayColor) {
        specialChars.Add(mode, Tuple.Create(specialMatcher, displayColor, new Regex($"\\{mode}[^\\s,]+")));
    }

    protected override void OnInputUpdate(string input) {
        if (input.Length > 0) {
            // highlighting
            highlighting = new Color[input.Length];
            for (int i = 0; i < highlighting.Length; i++)
                highlighting[i] = input[i] == ',' || input[i] == ';' ? Color.DarkGray : FG;
            foreach (var pair in specialChars) {
                foreach (Match match in pair.Value.Item3.Matches(input)) {
                    if (match.Success)
                        for (int i = match.Index; i < match.Index + match.Length; i++)
                            highlighting[i] = pair.Value.Item2;
                }
            }
        } else {
            Found = null;
            highlighting = null;
            base.OnInputUpdate(input);
            return;
        }

        if (Entries == null) {
            Found = null;
            base.OnInputUpdate(input);
            return;
        }

        // search pattern
        List<List<Tuple<char?, string>>> search = new();
        foreach (string termList in input.Split(',', ';')) {
            if (termList.Length != 0) {
                List<Tuple<char?, string>> l = new();

                foreach (string term in termList.Split(' ')) {
                    string t = term.Trim();
                    if (t.Length != 0) {
                        char c = t[0];
                        if (t.Length > 1 && specialChars.ContainsKey(c))
                            l.Add(Tuple.Create<char?, string>(c, t.Substring(1)));
                        else
                            l.Add(Tuple.Create<char?, string>(null, t));
                    }
                }

                if (l.Count != 0)
                    search.Add(l);
            }
        }

        // matching
        List<T> found = new List<T>();
        foreach (T entry in Entries) {
            bool matched = false;
            foreach (var terms in search) {
                // try 'AND' seq. of terms 
                bool m = true;
                foreach (var term in terms) {
                    bool check = term.Item1 == null ? termMatcher(entry, term.Item2) : specialChars[term.Item1.Value].Item1(entry, term.Item2);
                    if (!check) {
                        m = false;
                        break;
                    }
                }

                if (m) {
                    matched = true;
                    break; // this entry matches with this 'AND' seq., don't care about the others
                }
            }

            if (matched)
                found.Add(entry); // the search matched this entry
        }

        Found = found.ToArray();
        searchInfo = SearchInfo?.Invoke(Found.Length);

        base.OnInputUpdate(input);
    }
}