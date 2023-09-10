using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using Microsoft.Xna.Framework.Input;
using Snowberry.UI;

namespace Snowberry.Editor.Tools;

public class RoomTool : Tool {
    private Room lastSelected = null;
    private int lastFillerSelected = -1;
    public static bool ScheduledRefresh = false;

    private Vector2? lastRoomOffset = null;
    private static bool resizingX, resizingY, fromLeft, fromTop;
    private static int newWidth, newHeight;
    private static Rectangle oldRoomBounds;
    private static bool justSwitched = false;

    public static Rectangle? PendingRoom = null;

    public override UIElement CreatePanel(int height) {
        // room selection panel containing room metadata
        var ret = new UIRoomSelectionPanel {
            Width = 160,
            Height = height
        };
        ret.Refresh();
        return ret;
    }

    public override string GetName() => Dialog.Clean("SNOWBERRY_EDITOR_TOOL_ROOMS");

    public override void Update(bool canClick) {
        // refresh the display
        var curRoom = Editor.SelectedRoom;
        if (lastSelected != curRoom || lastFillerSelected != Editor.SelectedFillerIndex || ScheduledRefresh) {
            justSwitched = true;
            ScheduledRefresh = false;
            lastSelected = curRoom;
            lastFillerSelected = Editor.SelectedFillerIndex;
            if (Editor.Instance.ToolPanel is UIRoomSelectionPanel selectionPanel)
                selectionPanel.Refresh();
            if (curRoom != null) {
                lastRoomOffset = curRoom.Position - (Mouse.World / 8);
                oldRoomBounds = curRoom.Bounds;
            }
        }

        // move, resize, add rooms
        if (canClick && curRoom != null && !justSwitched) {
            if (MInput.Mouse.PressedLeftButton) {
                lastRoomOffset = curRoom.Position - (Mouse.World / 8);
                // check if the mouse is 8 pixels from the room's borders
                fromLeft = Math.Abs(Mouse.World.X / 8f - curRoom.Position.X) < 1;
                resizingX = Math.Abs(Mouse.World.X / 8f - (curRoom.Position.X + curRoom.Width)) < 1
                            || fromLeft;
                fromTop = Math.Abs(Mouse.World.Y / 8f - curRoom.Position.Y) < 1;
                resizingY = Math.Abs(Mouse.World.Y / 8f - (curRoom.Position.Y + curRoom.Height)) < 1
                            || fromTop;
                oldRoomBounds = curRoom.Bounds;
            } else if (MInput.Mouse.CheckLeftButton) {
                Vector2 world = Mouse.World / 8;
                var offset = lastRoomOffset ?? Vector2.Zero;
                if (!resizingX && !resizingY) {
                    var newX = (int)(world + offset).X;
                    var newY = (int)(world + offset).Y;
                    var diff = new Vector2(newX - curRoom.Bounds.X, newY - curRoom.Bounds.Y);
                    curRoom.Bounds.X = (int)(world + offset).X;
                    curRoom.Bounds.Y = (int)(world + offset).Y;
                    foreach (var e in curRoom.AllEntities) {
                        e.Move(diff * 8);
                        for (int i = 0; i < e.Nodes.Count; i++) {
                            e.MoveNode(i, diff * 8);
                        }
                    }
                } else {
                    int dx = 0, dy = 0;
                    if (resizingX) {
                        // compare against the opposite edge
                        newWidth = (int)Math.Ceiling(fromLeft ? oldRoomBounds.Right - world.X : world.X - curRoom.Bounds.Left);
                        curRoom.Bounds.Width = Math.Max(newWidth, 1);
                        if (fromLeft) {
                            int newX = (int)Math.Floor(world.X);
                            dx = curRoom.Bounds.X - newX;
                            curRoom.Bounds.X = newX;
                        }
                    }

                    if (resizingY) {
                        newHeight = (int)Math.Ceiling(fromTop ? oldRoomBounds.Bottom - world.Y : world.Y - curRoom.Bounds.Top);
                        curRoom.Bounds.Height = Math.Max(newHeight, 1);
                        if (fromTop) {
                            int newY = (int)Math.Floor(world.Y);
                            dy = curRoom.Bounds.Y - newY;
                            curRoom.Bounds.Y = newY;
                        }
                    }

                    // TODO: dragging over tiles and then back removes the tiles
                    //  maybe fix alongside undo/redo/transactions?
                    if (dx != 0 || dy != 0)
                        curRoom.MoveTiles(dx, dy);
                }
            } else {
                lastRoomOffset = null;
                var newBounds = curRoom.Bounds;
                if (!oldRoomBounds.Equals(newBounds)) {
                    oldRoomBounds = newBounds;
                    Editor.SelectedRoom.UpdateBounds();
                }

                resizingX = resizingY = fromLeft = fromTop = false;
                newWidth = newHeight = 0;
            }
        }

        if (MInput.Mouse.ReleasedLeftButton) {
            justSwitched = false;
        }

        // room creation
        if (canClick) {
            if (curRoom == null && Editor.SelectedFillerIndex == -1) {
                if (MInput.Mouse.CheckLeftButton) {
                    var lastPress = (Editor.Instance.worldClick / 8).Ceiling() * 8;
                    var mpos = (Mouse.World / 8).Ceiling() * 8;
                    int ax = (int)Math.Min(mpos.X, lastPress.X);
                    int ay = (int)Math.Min(mpos.Y, lastPress.Y);
                    int bx = (int)Math.Max(mpos.X, lastPress.X);
                    int by = (int)Math.Max(mpos.Y, lastPress.Y);
                    var newRoom = new Rectangle(ax, ay, bx - ax, by - ay);
                    if (newRoom.Width > 0 || newRoom.Height > 0) {
                        newRoom.Width = Math.Max(newRoom.Width, 8);
                        newRoom.Height = Math.Max(newRoom.Height, 8);
                        if (!PendingRoom.HasValue)
                            ScheduledRefresh = true;
                        PendingRoom = newRoom;
                    } else {
                        ScheduledRefresh = true;
                        PendingRoom = null;
                    }
                }
            } else {
                if (PendingRoom.HasValue) {
                    PendingRoom = null;
                    ScheduledRefresh = true;
                }
            }
        }
    }

    public override void RenderWorldSpace() {
        base.RenderWorldSpace();
        if (PendingRoom.HasValue) {
            var prog = (float)Math.Abs(Math.Sin(Engine.Scene.TimeActive * 3));
            var color = Color.Lerp(Color.White, Color.Cyan, prog) * 0.6f;
            Draw.Rect(PendingRoom.Value, color);
            Draw.HollowRect(PendingRoom.Value.X, PendingRoom.Value.Y, 40 * 8, 23 * 8, Color.Lerp(Color.Orange, Color.White, prog) * 0.6f);
            DrawUtil.DrawGuidelines(PendingRoom.Value, color);
        }

        bool viewRoomSize = Editor.Instance.CanTypeShortcut() && MInput.Keyboard.Check(Keys.D);
        if (Editor.SelectedRoom != null && (resizingX || resizingY || viewRoomSize))
            DrawUtil.DrawGuidelines(Editor.SelectedRoom.Bounds.Multiply(8), Color.White);
        else if (Editor.SelectedFillerIndex != -1 && viewRoomSize) {
            DrawUtil.DrawGuidelines(Editor.Instance.Map.Fillers[Editor.SelectedFillerIndex].Multiply(8), Color.White);
        }
    }

    public override void SuggestCursor(ref MTexture cursor, ref Vector2 justify) {
        var curRoom = Editor.SelectedRoom;
        Point mouse = new Point((int)Mouse.World.X, (int)Mouse.World.Y);
        // over another room/filler that isn't selected? just the default
        Room over = Editor.Instance.Map.GetRoomAt(mouse);
        if ((over != null && over != curRoom) || Editor.Instance.Map.GetFillerIndexAt(mouse) != -1) {
            return;
        }
        // all the other cursors are centred
        justify = Vector2.One / 2f;
        // over empty space? then a plus
        if (over == null) {
            cursor = Editor.CursorsAtlas.GetSubtexture(0, 16, 16, 16);
            return;
        }
        // if nothing is selected then these can be the only two
        if (curRoom == null)
            return;
        // then we must be hovered over the current room
        // resizing? use the appropriate cursor
        var fromLeft = Math.Abs(Mouse.World.X / 8f - curRoom.Position.X) < 1;
        var fromRight = Math.Abs(Mouse.World.X / 8f - (curRoom.Position.X + curRoom.Width)) < 1;
        var fromTop = Math.Abs(Mouse.World.Y / 8f - curRoom.Position.Y) < 1;
        var fromBottom = Math.Abs(Mouse.World.Y / 8f - (curRoom.Position.Y + curRoom.Height)) < 1;
        if ((fromBottom && fromLeft) || (fromTop && fromRight)) {
            cursor = Editor.CursorsAtlas.GetSubtexture(32, 32, 16, 16);
            return;
        }
        if ((fromTop && fromLeft) || (fromBottom && fromRight)) {
            cursor = Editor.CursorsAtlas.GetSubtexture(48, 32, 16, 16);
            return;
        }
        if (fromLeft || fromRight) {
            cursor = Editor.CursorsAtlas.GetSubtexture(0, 32, 16, 16);
            return;
        }
        if (fromBottom || fromTop) {
            cursor = Editor.CursorsAtlas.GetSubtexture(16, 32, 16, 16);
            return;
        }

        cursor = Editor.CursorsAtlas.GetSubtexture(16, 16, 16, 16);
    }
}