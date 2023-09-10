using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Snowberry;

public class Tree<T> : IEnumerable<T>{

    public T Value;
    public List<Tree<T>> Children = new();
    public Tree<T> Parent;

    public Tree(T value, params Tree<T>[] children) {
        Value = value;
        Children.AddRange(children);
    }

    public Tree<T> GetOrCreateChild(T t) {
        var found = Children.FirstOrDefault(x => t.Equals(x.Value));
        if (found != null)
            return found;

        Tree<T> child = new(t);
        Children.Add(child);
        child.Parent = this;
        return child;
    }

    public T AggregateUp(Func<T, T, T> combinator) {
        T v = Value;
        Tree<T> p = this;
        while ((p = p.Parent) != null)
            v = combinator(p.Value, v);
        return v;
    }

    public static Tree<T> FromPrefixes(List<List<T>> lists, T parent) {
        // https://stackoverflow.com/a/1005980 was helpful here
        Tree<T> root = new(parent);

        foreach (List<T> list in lists) {
            Tree<T> current = root;
            foreach (T value in list)
                current = current.GetOrCreateChild(value);
        }

        return root;
    }

    public IEnumerator<T> GetEnumerator() {
        yield return Value;
        foreach(T value in Children.SelectMany(t => t))
            yield return value;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}