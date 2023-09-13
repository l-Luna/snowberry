using System;
using System.Collections.Generic;
using System.Linq;

namespace Snowberry.Editor;

public class UndoRedo{

    // thanks C#!! very cool!!
    // type erased form of Snapshotter to allow hetrogenous collections
    public interface Snapshotter{
        public object SnapshotRaw();
        public void ApplyRaw(object o);
    }

    // and then what's actually exposed
    public class Snapshotter<T> : Snapshotter{
        public readonly Func<T> Snapshot;
        public readonly Action<T> Apply;

        public Snapshotter(Func<T> snapshot, Action<T> apply){
            Snapshot = snapshot;
            Apply = apply;
        }

        public Snapshotter<(T, U)> And<U>(Snapshotter<U> other) => new(
            () => (Snapshot(), other.Snapshot()),
            tuple => {
                Apply(tuple.Item1);
                other.Apply(tuple.Item2);
            }
        );

        public object SnapshotRaw() => Snapshot();
        public void ApplyRaw(object o) => Apply((T)o);
    }

    public static Snapshotter<object> OfAction(Action a) => new(() => null, _ => a());

    public class EditorAction{
        public string Name;
        public List<(Snapshotter snapshotter, object before, object after)> States = new();
        public bool Weak;

        public EditorAction(string name, bool weak = false){
            Name = name;
            Weak = weak;
        }

        public void Undo(){
            foreach(var state in States)
                state.snapshotter.ApplyRaw(state.before);
        }

        public void Redo(){
            foreach(var state in States)
                state.snapshotter.ApplyRaw(state.after);
        }

        internal void BackupRedoState(){
            var old = States;
            States = new(old.Count);
            foreach (var state in old)
                States.Add((state.snapshotter, state.before, state.snapshotter.SnapshotRaw()));
        }
    }

    // the list of actions that have been completed and possibly undone
    // does *not* include the current in-progress action
    private static readonly List<EditorAction> ActionLog = new();
    // the index into ActionLog that describes the current Editor state
    // if actions are undone, this moves back; without undoing, this is always `ActionLog.Count - 1`
    private static int CurActionIndex = -1;
    // the current in-progress action
    private static EditorAction InProgress = null;

    public static void BeginAction(string name, params Snapshotter[] snapshotters) {
        BeginAction(name, snapshotters.AsEnumerable());
    }

    public static void BeginAction(string name, IEnumerable<Snapshotter> snapshotters) {
        // either you have some undone actions (and nothing in progress)
        // or you have an in progress action (and are at the end of the log)
        // so the order these happen doesn't matter
        TrimLog();
        if(InProgress != null)
            if(InProgress.Weak)
                CompleteAction();
            else
                InProgress.Undo();

        InProgress = new EditorAction(name);
        foreach(var snapshotter in snapshotters)
            InProgress.States.Add((snapshotter, snapshotter.SnapshotRaw(), null));
    }

    public static void CompleteAction(){
        if(InProgress == null)
            throw new ArgumentException("CompleteAction can only be called while an action is in-progress!");

        InProgress.BackupRedoState();
        ActionLog.Add(InProgress);
        CurActionIndex++;
        InProgress = null;
    }

    private static void TrimLog(){
        if(CurActionIndex != ActionLog.Count - 1)
            ActionLog.RemoveRange(CurActionIndex + 1, ActionLog.Count - CurActionIndex - 1);
            // no need to set CurActionIndex since we've just forced ActionLog to match
    }

    public static void Undo(){
        if(CurActionIndex > -1){
            ActionLog[CurActionIndex].Undo();
            Snowberry.LogInfo("undid: " + ActionLog[CurActionIndex].Name);
            CurActionIndex--;
        }
    }

    public static void Redo(){
        if(CurActionIndex < ActionLog.Count - 1){
            CurActionIndex++;
            ActionLog[CurActionIndex].Redo();
            Snowberry.LogInfo("redid: " + ActionLog[CurActionIndex].Name);
        }
    }

    // it would kind of suck if anyone modified this improperly
    public static IReadOnlyList<EditorAction> ViewLog() => ActionLog.AsReadOnly();
}