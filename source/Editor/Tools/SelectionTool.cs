using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Snowberry.Editor.UI;
using System;
using System.Linq;
using Snowberry.Editor.UI.Menus;
using System.Collections.Generic;
using Celeste.Mod;

namespace Snowberry.Editor.Tools;

public class SelectionTool : Tool {
    private static bool canSelect;
    private static bool selectEntities = true, selectTriggers = true, selectFgDecals = false, selectBgDecals = false;
    private static UISelectionPane selectionPanel;

    // entity resizing
    private static bool resizingX, resizingY, fromLeft, fromTop;
    private static Rectangle oldEntityBounds;

    // paste preview
    private static bool pasting = false;
    private static List<Entity> toPaste;

    public override string GetName() => Dialog.Clean("SNOWBERRY_EDITOR_TOOL_SELECT");

    public override UIElement CreatePanel(int height) {
        UIElement panel = new UIElement {
            Width = 210,
            Background = Calc.HexToColor("202929") * (185 / 255f),
            GrabsClick = true,
            GrabsScroll = true,
            Height = height
        };

        panel.Add(selectionPanel = new UISelectionPane {
            Width = 210,
            Height = height - 30,
            Background = null
        });

        UIElement filtersPanel = new UIElement {
            Position = new Vector2(5, height - 20)
        };
        Vector2 offset = new Vector2(8, 0);
        filtersPanel.AddRight(UIPluginOptionList.BoolOption("entities", selectEntities, s => selectEntities = s));
        filtersPanel.AddRight(UIPluginOptionList.BoolOption("triggers", selectTriggers, s => selectTriggers = s), offset);
        filtersPanel.AddRight(UIPluginOptionList.BoolOption("fg", selectFgDecals, s => selectFgDecals = s), offset);
        filtersPanel.AddRight(UIPluginOptionList.BoolOption("bg", selectBgDecals, s => selectBgDecals = s), offset);
        panel.Add(filtersPanel);

        return panel;
    }

    public override void Update(bool canClick) {
        var editor = Editor.Instance;
        bool refreshPanel = false;

        if (pasting) {
            AdjustPastedEntities();

            if (MInput.Keyboard.Released(Keys.V)) {
                if (Editor.SelectedRoom != null)
                    foreach (Entity e in toPaste) {
                        e.EntityID = PlacementTool.AllocateId();
                        Editor.SelectedRoom.AddEntity(e);
                    }

                pasting = false;
                toPaste.Clear();
                UndoRedo.CompleteAction();
            }
        }

        if (MInput.Mouse.CheckLeftButton && canClick) {
            Point mouse = new Point((int)Editor.Mouse.World.X, (int)Editor.Mouse.World.Y);
            Vector2 world = Editor.Mouse.World;

            // double click -> select all of type
            if (Editor.SelectedRoom != null && Editor.Mouse.IsDoubleClick) {
                // first get everything under the mouse
                Editor.SelectedObjects = Editor.SelectedRoom.GetSelectedObjects(Editor.Mouse.World.ToRect(), selectEntities, selectTriggers, selectFgDecals, selectBgDecals);
                // then get all types of those entities
                HashSet<string> entityTypes = new(Editor.SelectedObjects.OfType<EntitySelection>().Select(x => x.Entity.Name));
                // clear the current selection
                Editor.SelectedObjects = new();
                // add back all entities of the same type
                foreach (var entity in Editor.SelectedRoom.AllEntities.Where(entity => entityTypes.Contains(entity.Name)))
                    if (entity.SelectionRectangles is { Length: > 0 } rs)
                        Editor.SelectedObjects.Add(new EntitySelection(entity, rs.Select((_, i) => new EntitySelection.SelectionRect(entity, i - 1)).ToList()));
                // we might have selected and/or deselected something, so refresh the panel
                refreshPanel = true;
            }

            if (MInput.Mouse.PressedLeftButton) {
                canSelect = !(Editor.SelectedObjects != null && Editor.SelectedObjects.Any(s => s.Contains(mouse)));
            }

            if (canSelect && Editor.SelectedRoom != null) {
                int ax = (int)Math.Min(Editor.Mouse.World.X, editor.worldClick.X);
                int ay = (int)Math.Min(Editor.Mouse.World.Y, editor.worldClick.Y);
                int bx = (int)Math.Max(Editor.Mouse.World.X, editor.worldClick.X);
                int by = (int)Math.Max(Editor.Mouse.World.Y, editor.worldClick.Y);
                Editor.SelectionInProgress = new Rectangle(ax, ay, bx - ax, by - ay);

                Editor.SelectedObjects = Editor.SelectedRoom.GetSelectedObjects(Editor.SelectionInProgress.Value, selectEntities, selectTriggers, selectFgDecals, selectBgDecals);
            } else if (Editor.SelectedObjects != null) {
                // if only one entity is selected near the corners, resize
                Entity solo = GetSoloEntity();
                if (solo != null) {
                    if (MInput.Mouse.PressedLeftButton) {
                        // TODO: can this be shared between RoomTool & SelectionTool?
                        fromLeft = Math.Abs(Editor.Mouse.World.X - solo.Position.X) <= 4;
                        resizingX = solo.MinWidth > -1 && (Math.Abs(Editor.Mouse.World.X - (solo.Position.X + solo.Width)) <= 4 || fromLeft);
                        fromTop = Math.Abs(Editor.Mouse.World.Y - solo.Position.Y) <= 4;
                        resizingY = solo.MinHeight > -1 && (Math.Abs(Editor.Mouse.World.Y - (solo.Position.Y + solo.Height)) <= 4 || fromTop);
                        oldEntityBounds = solo.Bounds;
                    } else if (resizingX || resizingY) {
                        var wSnapped = Editor.Mouse.World.RoundTo(8);
                        if (resizingX) {
                            // compare against the opposite edge
                            solo.SetWidth(Math.Max((int)Math.Round((fromLeft ? oldEntityBounds.Right - world.X : world.X - solo.X) / 8f) * 8, solo.MinWidth));
                            if (fromLeft)
                                solo.SetPosition(new((int)Math.Floor(wSnapped.X), solo.Y));
                        }

                        if (resizingY) {
                            solo.SetHeight(Math.Max((int)Math.Round((fromTop ? oldEntityBounds.Bottom - world.Y : world.Y - solo.Y) / 8f) * 8, solo.MinHeight));
                            if (fromTop)
                                solo.SetPosition(new(solo.X, (int)Math.Floor(wSnapped.Y)));
                        }

                        // skip dragging code
                        goto postResize;
                    }
                }

                // otherwise, move
                bool noSnap = MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl);
                Vector2 worldSnapped = noSnap ? Editor.Mouse.World : Editor.Mouse.World.RoundTo(8);
                Vector2 worldLastSnapped = noSnap ? Editor.Mouse.WorldLast : Editor.Mouse.WorldLast.RoundTo(8);
                Vector2 move = worldSnapped - worldLastSnapped;
                foreach (Selection s in Editor.SelectedObjects)
                    s.Move(move);
            }
        } else
            Editor.SelectionInProgress = null;

        if (Editor.SelectedObjects == null)
            return;

        postResize:
        if (Editor.Instance.CanTypeShortcut()) {
            bool ctrl = MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl);
            if (MInput.Keyboard.Check(Keys.Delete)) { // Del to delete entities
                foreach (var item in Editor.SelectedObjects) {
                    item.RemoveSelf();
                    refreshPanel = true;
                }

                Editor.SelectedObjects.Clear();
            } else if (MInput.Keyboard.Pressed(Keys.N)) { // N to create node
                bool did = false;
                // iterate backwards to allow modifying the list as we go
                for (var idx = Editor.SelectedObjects.Count - 1; idx >= 0; idx--) {
                    if(Editor.SelectedObjects[idx] is EntitySelection { Entity: var e } es && (e.Nodes.Count < e.MaxNodes || e.MaxNodes == -1)) {
                        if(!did) {
                            UndoRedo.BeginAction(Dialog.Clean("SNOWBERRY_ACTION_ADD_NODE"), Editor.SelectedObjects
                                .OfType<EntitySelection>()
                                .Select(x => x.Entity.SNodes()));
                            did = true;
                        }
                        int oldIdx = es.Selections[0].Index;
                        int newNodeIdx = oldIdx + 1;
                        Vector2 oldPos = oldIdx == -1 ? e.Position : e.Nodes[oldIdx];
                        e.AddNode(oldPos + new Vector2(24, 0), newNodeIdx);
                        Editor.SelectedObjects.Remove(es);
                        Editor.SelectedObjects.Add(new EntitySelection(e, new() { new(e, newNodeIdx) }));
                    }
                }
                if (did)
                    UndoRedo.CompleteAction();
            } else if (MInput.Keyboard.Pressed(Keys.Escape)) { // Esc to deselect all & cancel paste
                if (Editor.SelectedObjects.Count > 0)
                    refreshPanel = true;
                Editor.SelectedObjects.Clear();
                pasting = false;
                toPaste = null;
            } else if (MInput.Keyboard.Pressed(Keys.Up)) { // Up/Down/Left/Right to nudge entities
                Nudge(new(0, ctrl ? -1 : -8));
            } else if (MInput.Keyboard.Pressed(Keys.Down)) {
                Nudge(new(0, ctrl ? 1 : 8));
            } else if (MInput.Keyboard.Pressed(Keys.Left)) {
                Nudge(new(ctrl ? -1 : -8, 0));
            } else if (MInput.Keyboard.Pressed(Keys.Right)) {
                Nudge(new(ctrl ? 1 : 8, 0));
            } else if (Editor.SelectedRoom != null && ctrl) {
                if (MInput.Keyboard.Pressed(Keys.A)) { // Ctrl-A to select all
                    // select all
                    Editor.SelectedObjects = new();
                    foreach (var entity in Editor.SelectedRoom.AllEntities.Where(entity => (selectEntities && !entity.IsTrigger) || (selectTriggers && entity.IsTrigger)))
                        if (entity.SelectionRectangles is { Length: > 0 } rs)
                            Editor.SelectedObjects.Add(new EntitySelection(entity, rs.Select((_, i) => new EntitySelection.SelectionRect(entity, i - 1)).ToList()));
                    if (selectFgDecals)
                        foreach (Decal d in Editor.SelectedRoom.FgDecals)
                            Editor.SelectedObjects.Add(new DecalSelection(d, true));
                    if (selectBgDecals)
                        foreach (Decal d in Editor.SelectedRoom.BgDecals)
                            Editor.SelectedObjects.Add(new DecalSelection(d, false));
                    refreshPanel = true;
                } else if (MInput.Keyboard.Pressed(Keys.C)) { // Ctrl-C to copy
                    CopyPaste.Clipboard = CopyPaste.CopyEntities(Editor.SelectedObjects.OfType<EntitySelection>().Select(x => x.Entity));
                } else if (MInput.Keyboard.Pressed(Keys.V)) { // Ctrl-V to paste
                    try {
                        List<(EntityData data, bool trigger)> entities = CopyPaste.PasteEntities(CopyPaste.Clipboard);
                        if (entities.Count != 0) {
                            UndoRedo.BeginAction("pasting entities", Editor.SelectedRoom.SEntityList().And(UndoRedo.OfAction(() => pasting = false)));
                            pasting = true;
                            toPaste = new(entities.Count);
                            foreach (var entity in entities)
                                toPaste.Add(Entity.TryCreate(Editor.SelectedRoom, entity.data, entity.trigger, out bool _));
                            AdjustPastedEntities();
                        }
                    } catch (ArgumentException ae) {
                        Snowberry.LogInfo(ae.Message);
                    } catch (InvalidOperationException) {
                        Snowberry.LogInfo("Failed to paste (invalid data)");
                    }
                }
            }
        }

        if ((MInput.Mouse.ReleasedLeftButton && canClick && canSelect) || refreshPanel)
            selectionPanel?.Display(Editor.SelectedObjects);
    }

    public override void RenderWorldSpace() {
        base.RenderWorldSpace();
        if (Editor.SelectedRoom != null) {
            foreach (var item in Editor.SelectedRoom.GetSelectedObjects(Editor.Mouse.World.ToRect(), selectEntities, selectTriggers, selectFgDecals, selectBgDecals))
                if (Editor.SelectedObjects == null || !Editor.SelectedObjects.Contains(item))
                    foreach (var s in item.Rectangles())
                        Draw.Rect(s, Color.Blue * 0.15f);

            if (MInput.Mouse.CheckLeftButton && !canSelect && (resizingX || resizingY) && GetSoloEntity() is {} nonNull)
                DrawUtil.DrawGuidelines(nonNull.Bounds, Color.White);

            if (Editor.SelectedObjects != null && Editor.Instance.CanTypeShortcut() && MInput.Keyboard.Check(Keys.D))
                foreach (EntitySelection es in Editor.SelectedObjects.OfType<EntitySelection>())
                    DrawUtil.DrawGuidelines(es.Entity.Bounds, Color.White);

            if (pasting)
                foreach (Entity e in toPaste)
                    e.Render();
        }
    }

    public override void SuggestCursor(ref MTexture cursor, ref Vector2 justify) {
        Point mouse = new Point((int)Editor.Mouse.World.X, (int)Editor.Mouse.World.Y);

        // hovering over a selected entity? movement arrow
        if (Editor.SelectedObjects != null && Editor.SelectedObjects.Any(s => s.Contains(mouse))) {
            justify = Vector2.One / 2f;
            cursor = Editor.cursors.GetSubtexture(16, 16, 16, 16);

            // only have 1 entity selected & at the borders? show resizing tooltips
            Entity solo = GetSoloEntity();
            if (solo != null) {
                var fromLeft = solo.MinWidth > -1 && Math.Abs(Editor.Mouse.World.X - solo.Position.X) <= 4;
                var fromRight = solo.MinWidth > -1 && Math.Abs(Editor.Mouse.World.X - (solo.Position.X + solo.Width)) <= 4;
                var fromTop = solo.MinHeight > -1 && Math.Abs(Editor.Mouse.World.Y - solo.Position.Y) <= 4;
                var fromBottom = solo.MinHeight > -1 && Math.Abs(Editor.Mouse.World.Y - (solo.Position.Y + solo.Height)) <= 4;
                if (fromLeft || fromRight || fromTop || fromBottom) {
                    if ((fromBottom && fromLeft) || (fromTop && fromRight)) {
                        cursor = Editor.cursors.GetSubtexture(32, 32, 16, 16);
                        return;
                    }

                    if ((fromTop && fromLeft) || (fromBottom && fromRight)) {
                        cursor = Editor.cursors.GetSubtexture(48, 32, 16, 16);
                        return;
                    }

                    if (fromLeft || fromRight) {
                        cursor = Editor.cursors.GetSubtexture(0, 32, 16, 16);
                        return;
                    }

                    if (fromBottom || fromTop)
                        cursor = Editor.cursors.GetSubtexture(16, 32, 16, 16);
                }
            }
        }
    }

    private static Entity GetSoloEntity() {
        // list patterns would be nice here...
        if (Editor.SelectedObjects != null && Editor.SelectedObjects.Count == 1) {
            Selection selection = Editor.SelectedObjects[0];
            if (selection is EntitySelection es && es.Selections.Count == 1 && es.Selections[0].Index == -1)
                return es.Entity;
        }

        return null;
    }

    private static void AdjustPastedEntities() {
        Rectangle cover = CoveringRect(toPaste.Select(e => e.Bounds).Concat(toPaste.SelectMany(e => e.Nodes.Select(Util.ToRect))).ToList());
        Vector2 offset = (Editor.Mouse.World - cover.Center.ToVector2()).RoundTo(8);
        foreach(Entity e in toPaste){
            e.Move(offset);
            for (int i = 0; i < e.Nodes.Count; i++)
                e.MoveNode(i, offset);
        }
    }

    private static Rectangle CoveringRect(IReadOnlyList<Rectangle> rects) =>
        rects.Aggregate(rects[0], Rectangle.Union);

    private static void Nudge(Vector2 by) {
        if(Editor.SelectedObjects.Count == 0)
            return;
        UndoRedo.BeginAction("nudge objects", Editor.SelectedObjects.Select(x => x.SPositions()));
        foreach (Selection es in Editor.SelectedObjects)
            es.Move(by);
        UndoRedo.CompleteAction();
    }
}
