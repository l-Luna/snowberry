using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Snowberry.Editor.Entities;
using Snowberry.Editor.Recording;
using Snowberry.UI;
using Snowberry.UI.Controls;
using Snowberry.UI.Layout;
using Snowberry.UI.Menus;

namespace Snowberry.Editor;

public class Editor : UIScene {
    public class BufferCamera {
        private bool changedView = true;
        public void SetViewChanged() => changedView = true;

        private Vector2 pos;

        public Vector2 Position {
            get => pos;
            set {
                pos = value;
                changedView = true;
            }
        }

        public int X => (int)Position.X;
        public int Y => (int)Position.Y;

        private float scale = 1f;

        public float Zoom {
            get => scale;
            set {
                scale = value;
                changedView = true;
            }
        }

        private Matrix matrix, inverse, screenview;
        private Rectangle viewRect;

        public Matrix Matrix {
            get {
                if (changedView)
                    UpdateView();
                return matrix;
            }
        }

        public Matrix Inverse {
            get {
                if (changedView)
                    UpdateView();
                return inverse;
            }
        }

        public Matrix ScreenView {
            get {
                if (changedView)
                    UpdateView();
                return screenview;
            }
        }

        public Rectangle ViewRect {
            get {
                if (changedView)
                    UpdateView();
                return viewRect;
            }
        }

        public RenderTarget2D Buffer { get; private set; }

        public BufferCamera() {
            UpdateView();
        }

        private void UpdateView() {
            if (scale <= 1f)
                Buffer = null;
            else {
                Vector2 size = (new Vector2(Engine.ViewWidth, Engine.ViewHeight) / scale).Ceiling();

                if (Buffer == null || Buffer.IsDisposed || Buffer.Width != (int)size.X || Buffer.Height != (int)size.Y)
                    Buffer = new RenderTarget2D(Engine.Instance.GraphicsDevice, (int)size.X, (int)size.Y);
            }

            Matrix m = Matrix.CreateTranslation((int)-Position.X, (int)-Position.Y, 0f);

            screenview = m
                       * Matrix.CreateScale(Zoom)
                       * Matrix.CreateTranslation(Engine.ViewWidth / 2 + Engine.Viewport.X,
                                                  Engine.ViewHeight / 2 + Engine.Viewport.Y, 0f);

            m *= Matrix.CreateScale(Math.Min(1f, Zoom));

            if (Buffer != null) {
                m *= Matrix.CreateTranslation(Buffer.Width / 2, Buffer.Height / 2, 0f);
                viewRect = new Rectangle((int)Position.X - Buffer.Width / 2, (int)Position.Y - Buffer.Height / 2, Buffer.Width, Buffer.Height);
            } else {
                m *= Matrix.CreateTranslation(Engine.ViewWidth / 2 + Engine.Viewport.X, Engine.ViewHeight / 2 + Engine.Viewport.Y, 0f);
                int w = (int)(Engine.ViewWidth / Zoom);
                int h = (int)(Engine.ViewHeight / Zoom);
                viewRect = new Rectangle((int)Position.X - w / 2, (int)Position.Y - h / 2, w, h);
            }

            inverse = Matrix.Invert(matrix = m);

            changedView = false;
        }
    }

    public static new Editor Instance { get; private set; }

    // just forward to settings
    // don't expose setter here to make it explicit that changing this == changing user settings
    public static bool FancyRender => Snowberry.Settings.FancyRender;
    public static bool StylegroundsPreviews => Snowberry.Settings.StylegroundsPreview;

    public static readonly MTexture panningCursor = CursorsAtlas.GetSubtexture(32, 16, 16, 16);
    public bool Panning;
    public Vector2 PanWorldPos;

    public BufferCamera Camera { get; private set; }

    public Vector2 worldClick;

    public Map Map { get; private set; }

    internal static bool RoomSelectionLock;
    internal static Room SelectedRoom;
    internal static int SelectedFillerIndex = -1;
    internal static List<Selection> SelectedObjects = new();

    internal static DateTime? LastAutosave = null;

    public UIToolbar Toolbar;
    public UIElement ToolPanel, ToolPanelContainer;
    public UIElement ActionBar, ToolActionGroup;

    public Rectangle WorkArea;

    public UIPopOut HistoryWindow;
    public UIScrollPane HistoryLog;

    // TODO: potentially replace with just setting the MapData of Playtest
    private static bool generatePlaytestMapData = false;
    internal static Session PlaytestSession;
    internal static MapData PlaytestMapData;

    private static Vector2? lastPosition = null;
    private static float? lastZoom = null;

    public static int VanillaLevelID { get; private set; }
    public static AreaKey? From;
    public static string UnloadedBinName;

    private Editor(Map map) {
        Map = map;

        SelectedRoom = null;
        SelectedFillerIndex = -1;
        Instance = this;

        SaveData.InitializeDebugMode();
        UndoRedo.Reset();
    }

    internal static void Open(MapData data, bool rte = false) {
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
        Audio.Stop(Audio.CurrentMusicEventInstance);

        if (rte)
            RecInProgress.FinishRecording();
        else {
            RecInProgress.DiscardRecording();
            lastPosition = null; lastZoom = null;
        }

        Map map = null;
        if (data != null) {
            if (rte)
                Snowberry.Log(LogLevel.Info, $"Returning to editor for {From?.SID ?? "unnamed"}");
            else {
                Snowberry.Log(LogLevel.Info, $"Opening level editor using map {data.Area.GetSID()}");
                From = data.Area;
                TryBackup(Backups.BackupReason.OnOpen);
            }

            map = new Map(data);
            map.Rooms.ForEach(r => r.AllEntities.ForEach(e => e.InitializeAfter()));
        } else
            From = null;

        UnloadedBinName = null;
        Engine.Scene = new Editor(map);
    }

    internal static void OpenNew() {
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
        Audio.Stop(Audio.CurrentMusicEventInstance);
        RecInProgress.DiscardRecording();
        lastPosition = null; lastZoom = null;

        Snowberry.LogInfo("Opening new map in level editor");
        // Also empties the target's metadata.
        var map = new Map("snowberry map");
        map.Rooms.ForEach(r => r.AllEntities.ForEach(e => e.InitializeAfter()));
        From = null;
        UnloadedBinName = null;

        Engine.Scene = new Editor(map);
    }

    private void MappingUI() {
        Toolbar = new UIToolbar(this);
        UI.Add(Toolbar);
        Toolbar.Width = UIBuffer.Width;

        ActionBar = new() {
            Background = Calc.HexToColor("202929") * 0.4f,
            GrabsClick = true,
            Height = 33
        };
        UI.AddBelow(ActionBar);

        ActionBar.AddRight(new UILabel($"{From?.SID ?? "(new map)"}"), new Vector2(10, 12));

        if (From != null) {
            ActionBar.AddRight(new UILabel(From.Value.Mode switch {
                AreaMode.Normal => Dialog.Clean("SNOWBERRY_EDITOR_SIDE_A"),
                AreaMode.BSide => Dialog.Clean("SNOWBERRY_EDITOR_SIDE_B"),
                AreaMode.CSide => Dialog.Clean("SNOWBERRY_EDITOR_SIDE_C"),
                _ => "???"
            }) {
                Underline = true,
                FG = Color.Gold
            }, new Vector2(10, 11));
        }

        ActionBar.AddRight(new UIKeyboundButton(ActionbarAtlas.GetSubtexture(0, 0, 16, 16), 3, 3) {
            OnPress = BeginPlaytest,
            Ctrl = true,
            Key = Keys.P
        }, new(10, 4));

        ActionBar.AddRight(new UIKeyboundButton(ActionbarAtlas.GetSubtexture(16, 0, 16, 16), 3, 3) {
            OnPress = () => {
                if (UnloadedBinName == null && (From == null || Files.KeyToPath(From.Value) == null)) {
                    // show a popup asking for a filename to save to
                    Message.Clear();
                    // with a useful message
                    UILabel info = new UILabel(Dialog.Clean(From == null ? "SNOWBERRY_EDITOR_EXPORT_NEW" : "SNOWBERRY_EDITOR_EXPORT_UNSAVEABLE"));
                    info.Position = new Vector2(-info.Width / 2f, -28);
                    // validated textbox
                    UIValidatedTextField newName = new UIValidatedTextField(Fonts.Regular, 300) {
                        Error = true, // textfield starts off empty
                        CharacterBlacklist = Files.IllegalFilenameChars // just... don't type those
                    };
                    newName.Position = new Vector2(-newName.Width / 2f, -8);
                    newName.OnInputChange += s => newName.Error = !Files.IsValidFilename(s);
                    Message.AddElement(UIElement.Regroup(info, newName), new(0, -30), hiddenJustifyY: -0.1f);
                    Message.AddElement(UIMessage.YesAndNoButtons(() => {
                        string name = newName.Value;
                        if (Files.IsValidFilename(name)) {
                            BinaryExporter.ExportMapToFile(Map, newName.Value + ".bin");
                            Message.Shown = false;
                            UnloadedBinName = name;
                        }
                    }, () => Message.Shown = false), new(0, 24));
                    Message.Shown = true;
                } else if (UnloadedBinName != null) {
                    BinaryExporter.ExportMapToFile(Map, UnloadedBinName + ".bin");
                } else {
                    TryBackup(Backups.BackupReason.OnSave);
                    BinaryExporter.ExportMapToFile(Map);
                    // TODO: reload for loose map files
                    // if (From != null)
                    //     AssetReloadHelper.Do(Dialog.Clean("ASSETRELOADHELPER_RELOADINGMAP"), () => AreaData.Areas[From.Value.ID].Mode[0].MapData.Reload());
                }
            },
            Ctrl = true,
            Key = Keys.S
        }, new(6, 4));

        ActionBar.AddRight(new UIKeyboundButton(ActionbarAtlas.GetSubtexture(32, 0, 16, 16), 3, 3) {
            OnPress = () => {
                // TODO: show an "are you sure" message
                TryAutosave(Backups.BackupReason.OnClose);
                RecInProgress.DiscardRecording();
                Engine.Scene = new OverworldLoader(Overworld.StartMode.MainMenu);
            },
            Ctrl = true,
            Alt = true,
            Key = Keys.Q
        }, new(6, 4));

        if(From != null && Files.KeyToPath(From.Value) != null){
            ActionBar.AddRight(new UIKeyboundButton(ActionbarAtlas.GetSubtexture(48, 0, 16, 16), 3, 3) {
                OnPress = () => {
                    var backups = Backups.GetBackupsFor(From.Value);

                    Message.Clear();

                    UIScrollPane list = new() {
                        Width = 300,
                        Height = 400
                    };
                    bool odd = false;
                    foreach (Backups.Backup b in backups.OrderByDescending(x => x.Timestamp)) {
                        UIElement bg = new() {
                            Width = 300,
                            Height = 20,
                            Background = Color.Orange * (odd ? 0.4f : 0.5f)
                        };
                        UILabel label = new UILabel(Dialog.Get("SNOWBERRY_BACKUPS_DESC").Substitute(
                            Dialog.Clean("SNOWBERRY_BACKUPS_REASON_" + b.Reason.ToString().ToUpperInvariant()),
                            b.Timestamp
                        ));
                        bg.AddBelow(label, new((bg.Height - label.Height) / 2f));
                        UILabel filesize = new UILabel(Files.FormatFilesize(b.Filesize));
                        filesize.Position = new Vector2(bg.Width - filesize.Width - (bg.Height - filesize.Height) / 2f, (bg.Height - filesize.Height) / 2f);
                        bg.Add(filesize);
                        list.AddBelow(bg);
                        odd = !odd;
                    }

                    Message.AddElement(new UILabel(Dialog.Clean("SNOWBERRY_BACKUPS")), new(0, -220), hiddenJustifyY: -0.1f);
                    Message.AddElement(list, new(0, -8), hiddenJustifyY: -0.1f);

                    Message.AddElement(new UIButton(Dialog.Clean("SNOWBERRY_BACKUPS_OPEN_MAP_FOLDER"), Fonts.Regular, 3, 3) {
                        OnPress = () => {
                            string folder = Backups.BackupsDirectoryFor(From.Value);
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                            if (!folder.EndsWith("/")) folder += "/";
                            Process.Start("file://" + folder);
                        },
                        BG = Calc.HexToColor("ff8c00"),
                        HoveredBG = Calc.HexToColor("e37e02"),
                        PressedBG = Calc.HexToColor("874e07")
                    }, new(0, 215));
                    Message.AddElement(new UIButton(Dialog.Clean("SNOWBERRY_BACKUPS_DONE"), Fonts.Regular, 3, 3) {
                        OnPress = () => Message.Shown = false,
                        BG = Color.Red,
                        HoveredBG = Color.Crimson,
                        PressedBG = Color.DarkRed
                    }, new(0, 238));

                    Message.Shown = true;
                },
                Ctrl = true,
                Key = Keys.B
            }, new(6, 4));
        }

        ActionBar.AddRight(new UIButton(ActionbarAtlas.GetSubtexture(32, 96, 16, 16), 3, 3) {
            OnPress = () => {
                bool active = !HistoryWindow.Active;
                HistoryWindow.Active = HistoryWindow.Visible = active;
            }
        }, new(6, 4));

        UIElement RoomNameBar = new() {
            Height = Fonts.Regular.LineHeight
        };
        UI.AddBelow(RoomNameBar, new(10));

        RoomNameBar.Add(new UILabel(() => $"Room: {SelectedRoom?.Name ?? (SelectedFillerIndex > -1 ? $"(filler: {SelectedFillerIndex})" : "(none)")}"));
        RoomNameBar.AddRight(new UIKeyboundButton(ActionbarAtlas.GetSubtexture(64, 32, 16, 16), 3, 3) { // should be a toggle button
            OnPress = () => {
                RoomSelectionLock = !RoomSelectionLock;
            },
            GrabsClick = true, // doesn't work?
            Key = Keys.L // demonstration purposes only; i know this is a conflict
        }, new(6, -7));

        // anchor the tool panel under Message
        ToolPanelContainer = new() {
            Width = UI.Width,
            Height = UI.Height
        };
        UI.Add(ToolPanelContainer);

        SwitchTool(0);
    }

    public void BeginPlaytest() {
        lastPosition = Camera.Position;
        lastZoom = Camera.Zoom;

        if (Map.Rooms.Count == 0) {
            UIMessage.ShowInfoPopup("SNOWBERRY_EDITOR_PLAYTEST_NO_ROOMS", "SNOWBERRY_EDITOR_PLAYTEST_OK");
            return;
        }

        if (!Map.Rooms.Any(x => x.TrackedEntities.TryGetValue(typeof(Plugin_Player), out var p) && p.Any())) {
            UIMessage.ShowInfoPopup("SNOWBERRY_EDITOR_PLAYTEST_NO_SPAWNS", "SNOWBERRY_EDITOR_PLAYTEST_OK");
            return;
        }

        Audio.SetMusic(null);
        Audio.SetAmbience(null);

        // use debug save
        SaveData.InitializeDebugMode();
        // enable variants
        SaveData.Instance.VariantMode = true;
        SaveData.Instance.AssistMode = false;

        TryAutosave(Backups.BackupReason.OnPlaytest);

        generatePlaytestMapData = true;
        PlaytestMapData = new MapData(Map.From);
        PlaytestSession = new Session(Map.From);
        if ((SelectedRoom?.TrackedEntities.TryGetValue(typeof(Plugin_Player), out var players) ?? false) && players.FirstOrDefault() is { Position: var v }) {
            PlaytestSession.RespawnPoint = v;
            PlaytestSession.Level = SelectedRoom.Name;
            PlaytestSession.StartedFromBeginning = false;
        }

        /*
          TODO: need to re-apply map meta here to ensure edits made in the editor actually apply during playtest properly
          but this naive approach doesn't work, causes crashes in Xaphan Helper and others; something is setup inconsistently
        */
        // Map.Meta.ApplyTo(PlaytestMapData.Data);
        // foreach(ModeProperties prop in PlaytestMapData.Data.Mode)
        //     prop.MapData = PlaytestMapData;

        LevelEnter.Go(PlaytestSession, true);
        generatePlaytestMapData = false;
    }

    protected override void BeginContent() {
        Camera = new BufferCamera();
        if (lastZoom is { /* non-null */ } zoom)
            Camera.Zoom = zoom;
        if (lastPosition is { /* non-null */ } pos)
            Camera.Position = pos;

        if (Map == null)
            throw new Exception("Tried to open an Editor with no map! This used to open the main menu, but that is now a dedicated Scene!");

        MappingUI();
    }

    protected override void PostBeginContent() {
        base.PostBeginContent();

        HistoryWindow = new() {
            Title = Dialog.Clean("SNOWBERRY_EDITOR_HISTORY"),
            Width = 120,
            Height = 100,
            GrabsClick = true,
            GrabsScroll = true,
            // hidden by default
            Active = false,
            Visible = false
        };
        HistoryWindow.Add(HistoryLog = new UIScrollPane {
            Width = HistoryWindow.ContentWidth,
            Height = HistoryWindow.ContentHeight,
            Background = Color.Black
        });

        Overlay.Add(HistoryWindow);
        // HistoryWindow.Update();

        UndoRedo.OnChange += UpdateLog;
        UpdateLog();
    }

    public override void End() {
        base.End();
        Camera.Buffer?.Dispose();
        UndoRedo.OnChange -= UpdateLog;
    }

    protected override void UpdateContent() {
        float zoomScale = Camera.Zoom;

        // zooming
        bool canZoom = Mouse.IsFocused && UI.CanScrollThrough();
        int wheel = Math.Sign(MInput.Mouse.WheelDelta);
        if (wheel != 0) {
            if (canZoom) {
                if (wheel > 0)
                    zoomScale = zoomScale >= 1 ? zoomScale + 1 : zoomScale * 2f;
                else if (wheel < 0)
                    zoomScale = zoomScale > 1 ? zoomScale - 1 : zoomScale / 2f;
            }

            zoomScale = Calc.Clamp(zoomScale, 0.0625f, 24f);
        }

        // controls
        bool canClick = Mouse.IsFocused && UI.CanClickThrough() && !Message.Shown;

        // panning
        bool middlePan = Snowberry.Settings.MiddleClickPan;
        bool panButtonPressed = middlePan ? MInput.Mouse.CheckMiddleButton : MInput.Mouse.CheckRightButton;
        bool shouldWrap = Snowberry.Settings.PanWrapsMouse;
        if (panButtonPressed) {
            if (Panning) {
                var panDelta = PanWorldPos - Mouse.World;
                if (panDelta != Vector2.Zero) {
                    Camera.Position += panDelta;

                    var wrapScreenDelta = shouldWrap ? WrapMousePosition() : Vector2.Zero;
                    PanWorldPos = ScreenToWorld(Mouse.Screen + wrapScreenDelta);
                }
            } else if (canClick) {
                Panning = true;
                PanWorldPos = Mouse.World;
            }
        } else
            Panning = false;

        // room & filler select
        if ((MInput.Mouse.CheckLeftButton || MInput.Mouse.CheckRightButton) && canClick) {
            if (MInput.Mouse.PressedLeftButton || MInput.Mouse.PressedRightButton) {
                Point mouse = new Point((int)Mouse.World.X, (int)Mouse.World.Y);

                worldClick = Mouse.World;
                if (!RoomSelectionLock) {
                    var before = SelectedRoom;
                    SelectedRoom = Map.GetRoomAt(mouse);
                    SelectedFillerIndex = Map.GetFillerIndexAt(mouse);
                    // don't let tools click when clicking onto new rooms
                    if (SelectedRoom != before)
                        canClick = false;
                }
            }
        }

        // tool updating
        var tool = CurrentTool;
        tool.Update(canClick);

        // keybinds
        if (MInput.Keyboard.Check(Keys.LeftControl, Keys.RightControl) && CanTypeShortcut()) {
            bool saveSettings = false;
            if (MInput.Keyboard.Pressed(Keys.F)) {
                Snowberry.Settings.FancyRender = !Snowberry.Settings.FancyRender;
                saveSettings = true;
            }

            if (MInput.Keyboard.Pressed(Keys.L)) {
                Snowberry.Settings.StylegroundsPreview = !Snowberry.Settings.StylegroundsPreview;
                saveSettings = true;
            }

            if (saveSettings)
                Snowberry.Instance.SaveSettings();

            if (MInput.Keyboard.Pressed(Keys.Z))
                UndoRedo.Undo();
            if (MInput.Keyboard.Pressed(Keys.Y))
                UndoRedo.Redo();
        }

        // autosaving
        DateTime now = DateTime.Now;
        if (LastAutosave is null || LastAutosave.Value.AddMinutes(10) <= now) {
            TryAutosave(Backups.BackupReason.Autosave);
            LastAutosave = now; // in case we fail
        }

        // this happens at the end of the frame, to avoid coordinate conversions earlier being messed up
        if (zoomScale != Camera.Zoom) {
            var panScreenPos = WorldToScreen(PanWorldPos);
            Camera.Zoom = zoomScale;
            PanWorldPos = ScreenToWorld(panScreenPos);
        }
    }

    protected override Vector2 ScreenToWorld(Vector2 screenPos) => Vector2.Transform(
        (Camera.Buffer == null ? screenPos
                               : screenPos / Camera.Zoom)
        * UiScale,
        Camera.Inverse).Floor();

    protected override Vector2 WorldToScreen(Vector2 worldPos) =>
        Vector2.Transform(worldPos, Camera.Matrix).Floor()
        * (Camera.Buffer == null ? 1f : Camera.Zoom)
        / UiScale;

    protected Vector2 WrapMousePosition() => Mouse.Wrap(WorkArea);

    public void SwitchTool(int toolIdx) {
        ToolPanel?.RemoveSelf();

        Toolbar.CurrentTool = toolIdx;
        var tool = Tool.Tools[toolIdx];
        ToolPanel = tool.CreatePanel(UIBuffer.Height - Toolbar.Height);
        ToolPanel.Position = new Vector2(UIBuffer.Width - ToolPanel.Width, Toolbar.Height);
        ToolPanelContainer.Add(ToolPanel);

        ToolActionGroup?.RemoveSelf();
        ToolActionGroup = null;
        ActionBar.Update(); // get rid of the old action group immediately
        ActionBar.Width = UIBuffer.Width - ToolPanel.Width;
        var toolActionGroup = tool.CreateActionBar();
        if (toolActionGroup != null) {
            toolActionGroup.CalculateBounds(); // required for sub-elements to get any tooltips
            ToolActionGroup = new();
            ToolActionGroup.AddRight(new UILabel("|") {
                Position = new(5, 11)
            });
            var toolScrollPane = new UIScrollPane {
                Vertical = false,
                Height = ActionBar.Height,
                Background = null,
                BottomPadding = 5
            };
            toolScrollPane.AddRight(toolActionGroup);
            ToolActionGroup.AddRight(toolScrollPane, new Vector2(5, 0));
            toolScrollPane.CalculateBounds();
            ToolActionGroup.CalculateBounds(); // same
            ActionBar.AddRight(ToolActionGroup);
            toolScrollPane.Width = (int)(ActionBar.Width - ToolActionGroup.Position.X - 10);
        }

        WorkArea = new Rectangle(
            0, ActionBar.Bounds.Bottom,
            ActionBar.Width, UIBuffer.Height - ActionBar.Bounds.Bottom
        );

        SelectedObjects.Clear();
    }

    private void UpdateLog() {
        HistoryLog.Clear(now: true);
        var log = UndoRedo.ViewLog();
        for (var idx = 0; idx < log.Count; idx++)
            HistoryLog.AddBelow(RenderAction(log[idx], idx <= UndoRedo.ViewCurActionIdx(), idx % 2 == 0));
        if (UndoRedo.ViewInProgress() is { /* non-null */ } inProgress)
            HistoryLog.AddBelow(RenderAction(inProgress, null, true));
        HistoryLog.ClampToEnd();
    }

    private UIElement RenderAction(UndoRedo.EditorAction action, bool? done, bool odd) {
        UIElement box = new() {
            Width = HistoryLog.Width,
            Height = 12,
            Background = done switch {
                true => Color.DarkGreen,
                false => Color.DarkRed,
                null => Color.Gray
            } * (odd ? 0.5f : 0.25f)
        };
        box.AddRight(new UILabel(action.Name), new(1));

        return box;
    }

    public Tool CurrentTool => Tool.Tools[Toolbar.CurrentTool];

    protected override void RenderContent() {
        #region Map Rendering

        Engine.Instance.GraphicsDevice.SetRenderTarget(Camera.Buffer);

        Engine.Instance.GraphicsDevice.Clear(BG);
        Map.Render(Camera);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Camera.Matrix);
        CurrentTool.RenderWorldSpace();
        Draw.SpriteBatch.End();

        Map.PostRender();

        #endregion

        #region Displaying on Backbuffer + HQRender

        if (Camera.Buffer != null) {
            // draw the buffer onto the actual screen
            Engine.Instance.GraphicsDevice.SetRenderTarget(null);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            float scale = Engine.ViewWidth / (float)Camera.Buffer.Width;
            Draw.SpriteBatch.Draw(Camera.Buffer, new(Engine.Viewport.X, Engine.Viewport.Y), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            Draw.SpriteBatch.End();
        }

        Map.HQRender(Camera);

        // we've already drawn onto the screen, but what we've drawn might leak out of the drawable area
        // so we add back the black bars at the sides on top
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        Vector2 bufSize = new(Engine.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth,
                              Engine.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight);

        if (Engine.Viewport.X != 0) {
            Draw.Rect(new(0f, 0f), Engine.Viewport.X, bufSize.Y, Color.Black);
            Draw.Rect(new(bufSize.X - Engine.Viewport.X, 0f), Engine.Viewport.X, bufSize.Y, Color.Black);
        }
        if (Engine.Viewport.Y != 0) {
            Draw.Rect(new(0f, 0f), bufSize.X, Engine.Viewport.Y, Color.Black);
            Draw.Rect(new(0f, bufSize.Y - Engine.Viewport.Y), bufSize.X, Engine.Viewport.Y, Color.Black);
        }
        Draw.SpriteBatch.End();

        #endregion

        #region Tool Rendering

        if (CurrentTool != null) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(2));
            CurrentTool.RenderScreenSpace();
            Draw.SpriteBatch.End();
        }

        #endregion
    }

    protected override void SuggestCursor(ref MTexture texture, ref Vector2 justify) {
        bool canClick = UI.CanClickThrough() && !Message.Shown;

        if (Panning) {
            texture = panningCursor;
            justify = new(0.5f);
        } else if (canClick)
            CurrentTool?.SuggestCursor(ref texture, ref justify);
    }

    protected override void OnScreenResized() {
        Camera?.SetViewChanged();

        if(Toolbar != null) { // TODO: remove when the main menu becomes its own scene
            Toolbar.Width = UI.Width;
            ToolPanel.Position = new Vector2(UI.Width - ToolPanel.Width, Toolbar.Height);
            ToolPanel.Height = UI.Height;
            ActionBar.Width = UI.Width - ToolPanel.Width;
            Tool.Tools[Toolbar.CurrentTool].ResizePanel(UI.Height - Toolbar.Height);
        }

        WorkArea = new Rectangle(
            0, ActionBar.Bounds.Bottom,
            ActionBar.Width, UIBuffer.Height - ActionBar.Bounds.Bottom
        );
    }

    protected override bool ShouldShowUi() => !Input.MenuJournal.Check;

    public bool CanTypeShortcut() => !UI.NestedGrabsKeyboard();

    public static void TryBackup(Backups.BackupReason reason) {
        if(From != null) {
            string realPath = Files.KeyToPath(From.Value);
            if (File.Exists(realPath))
                Backups.SaveBackup(File.ReadAllBytes(realPath), From.Value, reason);
        }

        LastAutosave = DateTime.Now;
    }

    public static void TryAutosave(Backups.BackupReason reason) {
        if(From != null && Instance != null)
            Backups.SaveBackup(BinaryExporter.ExportToBytes(Instance.Map.Export(), From.Value.SID), From.Value, reason);

        LastAutosave = DateTime.Now;
    }

    // used reflectively
    private static void CreatePlaytestMapDataHook(Action<MapData> orig_Load, MapData self) {
        if (!generatePlaytestMapData)
            orig_Load(self);
        else {
            if (Engine.Scene is Editor editor) {
                editor.Map.GenerateMapData(self);
            } else orig_Load(self);
        }
    }

    private static MapData HookSessionGetAreaData(Func<Session, MapData> orig, Session self) {
        return self.Area.SID == "Snowberry/Playtest" ? PlaytestMapData : orig(self);
    }

    internal static void CopyAreaData(AreaData from, AreaData to) {
        to.ASideAreaDataBackup = from.ASideAreaDataBackup;
        to.BloomBase = from.BloomBase;
        to.BloomStrength = from.BloomStrength;
        to.CanFullClear = from.CanFullClear;
        to.CassetteSong = from.CassetteSong;
        to.CobwebColor = from.CobwebColor;
        to.ColorGrade = from.ColorGrade;
        to.CompleteScreenName = from.CompleteScreenName;
        to.CoreMode = from.CoreMode;
        to.CrumbleBlock = from.CrumbleBlock;
        to.DarknessAlpha = from.DarknessAlpha;
        to.Dreaming = from.Dreaming;
        to.Icon = from.Icon;
        to.Interlude = from.Interlude;
        to.IntroType = from.IntroType;
        to.IsFinal = from.IsFinal;
        to.Jumpthru = from.Jumpthru;
        to.Meta = from.Meta;
        to.Mode = from.Mode;
        to.Name = from.Name;
        to.Spike = from.Spike;
        // mountain meta?
        to.TitleAccentColor = from.TitleAccentColor;
        to.TitleBaseColor = from.TitleBaseColor;
        to.TitleTextColor = from.TitleTextColor;
        to.Wipe = from.Wipe;
        to.WoodPlatform = from.WoodPlatform;

        // hold onto info about vanilla's hardcoded stuff
        VanillaLevelID = from.IsOfficialLevelSet() ? from.ID : -1;
    }
}