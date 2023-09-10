using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using static Snowberry.UI.UIDropdown;

namespace Snowberry.UI;

public class UIMultiDropdown<T> : UIElement {

    // basic strategy: handle a bunch of non-flimsy UIDropdowns
    // store them in a list and check their integrity along the way

    public readonly Tree<T> Values;
    public readonly Func<Tree<T>, DropdownEntry> Display;

    private readonly List<UIDropdown> steps = new();

    public UIMultiDropdown(Tree<T> values, Func<Tree<T>, DropdownEntry> display) {
        Values = values;
        Display = display;

        UIDropdown first = BuildStep(Values);
        Add(first);
        steps.Add(first);
    }

    public override void Update(Vector2 position = default) {
        base.Update(position);

        if (steps[0].Destroyed) {
            RemoveSelf();
            return;
        }

        for (var idx = 0; idx < steps.Count; idx++) {
            var step = steps[idx];
            int hovered = step.FindHoverIdx(position + step.Position);
            if (hovered != -1) {
                var tag = step.Entries[hovered].Tag;
                Tree<T> path = tag as Tree<T>
                               ?? throw new InvalidOperationException($"Entry {hovered} of step {idx} of UIMultiDropdown had {tag} tag!");
                // if the next one is wrong,
                if(steps.Count <= idx + 1 && path.Children.Count > 0 // should have a child but there is none
                   || (steps.Count > idx + 1 && steps[idx + 1].Tag is Tree<T> parent && parent != path)){ // we have a child and it's parent is wrong
                    // clear everything after this step
                    if (steps.Count > idx + 1) {
                        for (int i = 0; i < steps.Count - idx - 1; i++) {
                            var index = idx + 1 + i;
                            steps[index].Destroy();
                            RemoveNow(steps[index]);
                            steps.RemoveAt(index);
                        }
                    }

                    // and add the next step
                    if (path.Children.Count > 0) {
                        var next = BuildStep(path);
                        next.Position = step.Position + new Vector2(step.Width, step.YPosFor(hovered));
                        steps.Add(next);
                        Add(next);
                    }
                }
            }
        }
    }

    private UIDropdown BuildStep(Tree<T> step) =>
        new(Fonts.Regular, step.Children
            .Select(x => {
                var u = Display(x);
                u.Tag = x;
                return u;
            })
            .OrderBy(x => x.Label)
            .ToArray())
        {
            Tag = step
        };
}