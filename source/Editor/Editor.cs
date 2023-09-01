using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Snowberry.Editor.UI;
using Snowberry.Editor.UI.Menus;

namespace Snowberry.Editor;

public class Editor : Scene {
    public class BufferCamera {
        private bool changedView = true;

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
                if (scale < 1f)
                    Buffer = null;
                else {
                    Vector2 size = new Vector2(Engine.Width, Engine.Height) / scale;
                    Buffer = new RenderTarget2D(Engine.Instance.GraphicsDevice, (int)size.X + (Engine.Width % scale == 0 ? 0 : 1), (int)size.Y + (Engine.Height % scale == 0 ? 0 : 1));
                }

                changedView = true;
            }
        }

        private Matrix matrix, inverse, screenview;

        public Matrix Matrix {
            get {
                if (changedView)
                    UpdateMatrices();
                return matrix;
            }
        }

        public Matrix Inverse {
            get {
                if (changedView)
                    UpdateMatrices();
                return inverse;
            }
        }

        public Matrix ScreenView {
            get {
                if (changedView)
                    UpdateMatrices();
                return screenview;
            }
        }

        public Rectangle ViewRect { get; private set; }

        public RenderTarget2D Buffer { get; private set; }

        public BufferCamera() {
            Buffer = new RenderTarget2D(Engine.Instance.GraphicsDevice, Engine.Width, Engine.Height);
        }

        private void UpdateMatrices() {
            Matrix m = Matrix.CreateTranslation((int)-Position.X, (int)-Position.Y, 0f) * Matrix.CreateScale(Math.Min(1f, Zoom));

            if (Buffer != null) {
                m *= Matrix.CreateTranslation(Buffer.Width / 2, Buffer.Height / 2, 0f);
                ViewRect = new Rectangle((int)Position.X - Buffer.Width / 2, (int)Position.Y - Buffer.Height / 2, Buffer.Width, Buffer.Height);
                screenview = m * Matrix.CreateScale(Zoom);
            } else {
                m *= Engine.ScreenMatrix * Matrix.CreateTranslation(Engine.ViewWidth / 2, Engine.ViewHeight / 2, 0f);
                int w = (int)(Engine.Width / Zoom);
                int h = (int)(Engine.Height / Zoom);
                ViewRect = new Rectangle((int)Position.X - w / 2, (int)Position.Y - h / 2, w, h);
                screenview = m;
            }

            inverse = Matrix.Invert(matrix = m);

            changedView = false;
        }
    }

    public static class Mouse {
        public static Vector2 Screen { get; internal set; }
        public static Vector2 ScreenLast { get; internal set; }

        public static Vector2 World { get; internal set; }
        public static Vector2 WorldLast { get; internal set; }

        public static DateTime LastClick { get; internal set; }
        public static bool IsDoubleClick => MInput.Mouse.PressedLeftButton && DateTime.Now < LastClick.AddMilliseconds(200);
    }

    public static Editor Instance { get; private set; }

    // just forward to settings
    // don't expose setter here to make it explicit that changing this == changing user settings
    public static bool FancyRender => Snowberry.Settings.FancyRender;
    public static bool StylegroundsPreviews => Snowberry.Settings.StylegroundsPreview;

    public static readonly MTexture cursors = GFX.Gui["Snowberry/cursors"];
    public static readonly Color bg = Calc.HexToColor("060607");
    private readonly MTexture defaultCursor = cursors.GetSubtexture(0, 0, 16, 16);
    private readonly MTexture panningCursor = cursors.GetSubtexture(32, 16, 16, 16);

    private bool fadeIn = false;
    public BufferCamera Camera { get; private set; }

    public Vector2 mousePos, lastMousePos;
    public Vector2 worldClick;
    public static bool MouseClicked = false;

    public Map Map { get; private set; }

    private RenderTarget2D uiBuffer;
    private readonly UIElement ui = new();
    public static UIMessage Message { get; private set; }

    internal static Rectangle? SelectionInProgress;
    internal static Room SelectedRoom;
    internal static int SelectedFillerIndex = -1;
    internal static List<Selection> SelectedObjects;

    public UIToolbar Toolbar;
    public UIElement ToolPanel;

    // TODO: potentially replace with just setting the MapData of Playtest
    private static bool generatePlaytestMapData = false;
    internal static Session PlaytestSession;
    internal static MapData PlaytestMapData;

    public static int VanillaLevelID { get; private set; }
    public static AreaKey? From;

    private Editor(Map map) {
        Map = map;

        SelectedRoom = null;
        SelectedFillerIndex = -1;
        Instance = this;

        SaveData.InitializeDebugMode();
    }

    internal static void Open(MapData data) {
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
        Audio.Stop(Audio.CurrentMusicEventInstance);

        Map map = null;
        if (data != null) {
            Snowberry.Log(LogLevel.Info, $"Opening level editor using map {data.Area.GetSID()}");
            // Also copies the target's metadata into Playtest's metadata.
            From = data.Area;
            map = new Map(data);
            map.Rooms.ForEach(r => r.AllEntities.ForEach(e => e.InitializeAfter()));
        } else
            From = null;

        Engine.Scene = new Editor(map);
    }

    internal static void OpenNew() {
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
        Audio.Stop(Audio.CurrentMusicEventInstance);

        Snowberry.LogInfo("Opening new map in level editor");
        // Also empties the target's metadata.
        var map = new Map("snowberry map");
        map.Rooms.ForEach(r => r.AllEntities.ForEach(e => e.InitializeAfter()));
        From = null;

        Engine.Scene = new Editor(map);
    }

    internal static void OpenFancy(MapData data)
    {
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
        Audio.Stop(Audio.CurrentMusicEventInstance);
        Map map = null;
        if(data != null)
        {
            Snowberry.Log(LogLevel.Info, $"Opening level editor using map {data.Area.GetSID()}");
            map = new Map(data);
        }

        var _ = new FadeWipe(Engine.Scene, false, () => {
            Editor e = new(map) {
                fadeIn = true
            };
            Engine.Scene = e;
        }) {
            Duration = data != null ? 0.3f : 0.85f
        };
    }

    private void MenuUI() {
        ui.Add(new UIMainMenu(uiBuffer.Width, uiBuffer.Height));
    }

    private void MappingUI() {
        Toolbar = new UIToolbar(this);
        ui.Add(Toolbar);
        Toolbar.Width = uiBuffer.Width;

        var nameLabel = new UILabel($"Map: {From?.SID ?? "(new map)"} (ID: {From?.ID ?? -1}, Mode: {From?.Mode ?? AreaMode.Normal})");
        ui.AddBelow(nameLabel);
        nameLabel.Position += new Vector2(10, 10);

        var roomLabel = new UILabel(() => $"Room: {SelectedRoom?.Name ?? (SelectedFillerIndex > -1 ? $"(filler: {SelectedFillerIndex})" : "(none)")}");
        ui.AddBelow(roomLabel);
        roomLabel.Position += new Vector2(10, 10);

        string editorreturn = Dialog.Clean("SNOWBERRY_EDITOR_RETURN");
        string editorplaytest = Dialog.Clean("SNOWBERRY_EDITOR_PLAYTEST");
        string editorexport = Dialog.Clean("SNOWBERRY_EDITOR_EXPORT");

        if (From.HasValue) {
            UIButton rtm = new UIButton(editorreturn, Fonts.Regular, 6, 6) {
                OnPress = () => {
                    Audio.SetMusic(null);
                    Audio.SetAmbience(null);

                    SaveData.InitializeDebugMode();

                    LevelEnter.Go(new Session(From.Value), true);
                }
            };
            ui.AddBelow(rtm);
        }

        UIButton test = new UIButton(editorplaytest, Fonts.Regular, 6, 6) {
            OnPress = () => {
                Audio.SetMusic(null);
                Audio.SetAmbience(null);

                SaveData.InitializeDebugMode();

                generatePlaytestMapData = true;
                PlaytestMapData = new MapData(Map.From);
                PlaytestSession = new Session(Map.From);
                LevelEnter.Go(PlaytestSession, true);
                generatePlaytestMapData = false;
            }
        };
        ui.AddBelow(test);

        UIButton export = new UIButton(editorexport, Fonts.Regular, 6, 6) {
            OnPress = () => {
                if (From == null || Util.KeyToPath(From.Value) == null) {
                    // show a popup asking for a filename to save to
                    Message.Clear();
                    // with a useful message
                    UILabel info = new UILabel(Dialog.Clean(From == null ? "SNOWBERRY_EDITOR_EXPORT_NEW" : "SNOWBERRY_EDITOR_EXPORT_UNSAVEABLE"));
                    info.Position = new Vector2(-info.Width / 2f, -28);
                    // TODO: validate that it's a valid filename & doesn't already exist
                    UITextField newName = new UITextField(Fonts.Regular, 300);
                    newName.Position = new Vector2(-newName.Width / 2f, -8);
                    // TODO: this UI code sucks
                    var element = UIElement.Regroup(info, newName);
                    Vector2 offset = new Vector2(element.Width / 2f, element.Height);
                    info.Position -= offset; newName.Position -= offset;
                    Message.AddElement(element, 0.5f, 0.5f, 0.5f, -0.1f);
                    var buttons = UIMessage.YesAndNoButtons(() => {
                        // no point auto-reloading when the map definitely doesn't exist yet
                        BinaryExporter.ExportMap(Map, newName.Value + ".bin");
                        Message.Shown = false;
                    }, () => Message.Shown = false, 0, 4, 0.5f);
                    Message.AddElement(buttons, 0.5f, 0.5f, 0.5f, 1.1f);
                    Message.Shown = true;
                } else {
                    BinaryExporter.ExportMap(Map);
                    if(From != null)
                        AssetReloadHelper.Do(Dialog.Clean("ASSETRELOADHELPER_RELOADINGMAP"), () => AreaData.Areas[From.Value.ID].Mode[0].MapData.Reload());
                }
            }
        };
        ui.AddBelow(export);

        SwitchTool(0);
    }

    public override void Begin() {
        base.Begin();

        Camera = new BufferCamera();

        uiBuffer = new RenderTarget2D(Engine.Instance.GraphicsDevice, Engine.ViewWidth / 2, Engine.ViewHeight / 2);
        ui.Width = uiBuffer.Width;
        ui.Height = uiBuffer.Height;

        if (Map == null)
            MenuUI();
        else
            MappingUI();

        ui.Add(Message = new UIMessage {
            Width = ui.Width,
            Height = ui.Height
        });
    }

    public override void End() {
        base.End();
        Camera.Buffer?.Dispose();
        uiBuffer.Dispose();
        ui.Destroy();
    }

    public override void Update() {
        base.Update();

        Mouse.WorldLast = Mouse.World;
        Mouse.ScreenLast = Mouse.Screen;

        lastMousePos = mousePos;
        mousePos = MInput.Mouse.Position;

        // zooming
        bool canZoom = ui.CanScrollThrough();
        int wheel = Math.Sign(MInput.Mouse.WheelDelta);
        if (wheel != 0) {
            float scale = Camera.Zoom;
            if (canZoom) {
                if (wheel > 0)
                    scale = scale >= 1 ? scale + 1 : scale * 2f;
                else if (wheel < 0)
                    scale = scale > 1 ? scale - 1 : scale / 2f;
            }

            scale = Calc.Clamp(scale, 0.0625f, 24f);
            if (scale != Camera.Zoom)
                Camera.Zoom = scale;
        }

        if (Camera.Buffer != null)
            mousePos /= Camera.Zoom;

        // controls
        bool canClick = ui.CanClickThrough() && !Message.Shown;

        // panning
        bool middlePan = Snowberry.Settings.MiddleClickPan;
        if ((middlePan && MInput.Mouse.CheckMiddleButton || !middlePan && MInput.Mouse.CheckRightButton) && canClick) {
            Vector2 move = lastMousePos - mousePos;
            if (move != Vector2.Zero)
                Camera.Position += move / (Camera.Buffer == null ? Camera.Zoom : 1f);
        }

        MouseState m = Microsoft.Xna.Framework.Input.Mouse.GetState();
        Vector2 mouseVec = new Vector2(m.X, m.Y);
        Mouse.Screen = mouseVec / 2;
        Mouse.World = Vector2.Transform(Camera.Buffer == null ? mouseVec : mousePos, Camera.Inverse).Floor();

        MouseClicked = false;
        ui.Update();

        // room & filler select
        if (Map != null) {
            if ((MInput.Mouse.CheckLeftButton || MInput.Mouse.CheckRightButton) && canClick) {
                if (MInput.Mouse.PressedLeftButton || MInput.Mouse.PressedRightButton) {
                    Point mouse = new Point((int)Mouse.World.X, (int)Mouse.World.Y);

                    worldClick = Mouse.World;
                    var before = SelectedRoom;
                    SelectedRoom = Map.GetRoomAt(mouse);
                    SelectedFillerIndex = Map.GetFillerIndexAt(mouse);
                    // don't let tools click when clicking onto new rooms
                    if (SelectedRoom != before)
                        canClick = false;
                }
            }

            // tool updating
            var tool = Tool.Tools[Toolbar.CurrentTool];
            tool.Update(canClick);

            // keybinds
            if (CanTypeShortcut() && (MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl))) {
                bool saveSettings = false;
                if (MInput.Keyboard.Pressed(Keys.F)) {
                    Snowberry.Settings.FancyRender = !Snowberry.Settings.FancyRender;
                    saveSettings = true;
                }
                if (MInput.Keyboard.Pressed(Keys.P)) {
                    Snowberry.Settings.StylegroundsPreview = !Snowberry.Settings.StylegroundsPreview;
                    saveSettings = true;
                }

                if (MInput.Keyboard.Pressed(Keys.Z))
                    UndoRedo.Undo();
                if (MInput.Keyboard.Pressed(Keys.Y))
                    UndoRedo.Redo();

                if (saveSettings)
                    Snowberry.Instance.SaveSettings();
            }
        }

        // Double click
        if (MInput.Mouse.PressedLeftButton) {
            Mouse.LastClick = DateTime.Now;
        }
    }

    public void SwitchTool(int toolIdx) {
        ToolPanel?.Destroy();
        ui.Remove(ToolPanel);

        Toolbar.CurrentTool = toolIdx;
        var tool = Tool.Tools[toolIdx];
        ToolPanel = tool.CreatePanel(uiBuffer.Height - Toolbar.Height);
        ToolPanel.Position = new Vector2(uiBuffer.Width - ToolPanel.Width, Toolbar.Height);
        ui.Add(ToolPanel);

        SelectedObjects = null;
    }

    public override void Render() {
        Draw.SpriteBatch.GraphicsDevice.Clear(bg);

        var tool = Map == null ? null : Tool.Tools[Toolbar.CurrentTool];
        bool canClick = ui.CanClickThrough() && !Message.Shown;
        bool middlePan = Snowberry.Settings.MiddleClickPan;
        var panning = (middlePan && MInput.Mouse.CheckMiddleButton || !middlePan && MInput.Mouse.CheckRightButton) && canClick;

        #region UI Rendering

        Engine.Instance.GraphicsDevice.SetRenderTarget(uiBuffer);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

        ui.Render();

        MTexture curCursor = defaultCursor;
        Vector2 curJustify = Vector2.Zero;
        if (panning) {
            curCursor = panningCursor;
            curJustify = new(0.5f);
        } else if(canClick)
            tool?.SuggestCursor(ref curCursor, ref curJustify);
        curCursor.DrawJustified(Mouse.Screen, curJustify);

        // Tooltip rendering
        var tooltip = ui.HoveredTooltip();
        if (tooltip != null) {
            string[] array = tooltip.Split(new[] { "\\n" }, StringSplitOptions.None);
            for(int i = 0; i < array.Length; i++) {
                string line = array[i];
                var tooltipArea = Fonts.Regular.Measure(line);
                var at = Mouse.Screen.Round() - new Vector2((tooltipArea.X + 8), -(tooltipArea.Y + 6) * i);
                Draw.Rect(at, tooltipArea.X + 8, tooltipArea.Y + 6, Color.Black * 0.8f);
                Fonts.Regular.Draw(line, at + new Vector2(4, 3), Vector2.One, Color.White);
            }
        }

        Draw.SpriteBatch.End();

        #endregion

        #region Tool Rendering

        if (Map != null) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            tool.RenderScreenSpace();
            Draw.SpriteBatch.End();
        }

        #endregion

        #region Map Rendering

        if (Camera.Buffer != null)
            Engine.Instance.GraphicsDevice.SetRenderTarget(Camera.Buffer);
        else
            Engine.Instance.GraphicsDevice.SetRenderTarget(null);

        Engine.Instance.GraphicsDevice.Clear(bg);
        if (Map != null) {
            Map.Render(Camera);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Camera.Matrix);
            tool.RenderWorldSpace();
            Draw.SpriteBatch.End();
        }

        #endregion

        #region Displaying on Backbuffer + HQRender

        if (Camera.Buffer != null) {
            Engine.Instance.GraphicsDevice.SetRenderTarget(null);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
            Draw.SpriteBatch.Draw(Camera.Buffer, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Camera.Zoom, SpriteEffects.None, 0f);
            Draw.SpriteBatch.End();
        }

        // HQRender
        Map?.HQRender(Camera);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        Draw.SpriteBatch.Draw(uiBuffer, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One * 2, SpriteEffects.None, 0f);
        Draw.SpriteBatch.End();

        #endregion
    }

    public bool CanTypeShortcut() => !ui.NestedGrabsKeyboard();

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

    internal static void CopyMapMeta(MapMeta from, MapMeta to){
        to.Parent = from.Parent;
        to.Icon = from.Icon;
        to.Interlude = from.Interlude;
        to.CassetteCheckpointIndex = from.CassetteCheckpointIndex;
        to.TitleBaseColor = from.TitleBaseColor;
        to.TitleAccentColor = from.TitleAccentColor;
        to.TitleTextColor = from.TitleTextColor;
        to.IntroType = from.IntroType;
        to.Dreaming = from.Dreaming;
        to.ColorGrade = from.ColorGrade;
        to.Wipe = from.Wipe;
        to.DarknessAlpha = from.DarknessAlpha;
        to.BloomBase = from.BloomBase;
        to.BloomStrength = from.BloomStrength;
        to.Jumpthru = from.Jumpthru;
        to.CoreMode = from.CoreMode;
        to.CassetteNoteColor = from.CassetteNoteColor;
        to.CassetteSong = from.CassetteSong;
        to.PostcardSoundID = from.PostcardSoundID;
        to.ForegroundTiles = from.ForegroundTiles;
        to.BackgroundTiles = from.BackgroundTiles;
        to.AnimatedTiles = from.AnimatedTiles;
        to.Sprites = from.Sprites;
        to.Portraits = from.Portraits;
        to.OverrideASideMeta = from.OverrideASideMeta;

        if(from.CassetteModifier != null)
            to.CassetteModifier = new MapMetaCassetteModifier{
                TempoMult = from.CassetteModifier.TempoMult,
                LeadBeats = from.CassetteModifier.LeadBeats,
                BeatsPerTick = from.CassetteModifier.BeatsPerTick,
                TicksPerSwap = from.CassetteModifier.TicksPerSwap,
                Blocks = from.CassetteModifier.Blocks,
                BeatsMax = from.CassetteModifier.BeatsMax,
                BeatIndexOffset = from.CassetteModifier.BeatIndexOffset,
                OldBehavior = from.CassetteModifier.OldBehavior
            };

        /*to.Mountain = new MapMetaMountain{
            MountainModelDirectory = from.Mountain.MountainModelDirectory,
            MountainTextureDirectory = from.Mountain.MountainTextureDirectory,
            BackgroundMusic = from.Mountain.BackgroundMusic,
            BackgroundAmbience = from.Mountain.BackgroundAmbience,
            BackgroundMusicParams = new Dictionary<string, float>(from.Mountain.BackgroundMusicParams),
            FogColors = Copy(from.Mountain.FogColors),
            StarFogColor = from.Mountain.StarFogColor,
            StarStreamColors = Copy(from.Mountain.StarStreamColors),
            StarBeltColors1 = Copy(from.Mountain.StarBeltColors1),
            StarBeltColors2 = Copy(from.Mountain.StarBeltColors2),
            Idle = Copy(from.Mountain.Idle),
            Select = Copy(from.Mountain.Select),
            Zoom = Copy(from.Mountain.Zoom),
            Cursor = Copy(from.Mountain.Cursor),
            State = from.Mountain.State,
            Rotate = from.Mountain.Rotate,
            ShowCore = from.Mountain.ShowCore,
            ShowSnow = from.Mountain.ShowSnow
        };*/ // other non-gameplay attributes don't need to be handled here
    }

    internal static void EmptyMapMeta(AreaData of) {
        CopyAreaData(new AreaData(), of);
    }

    /*internal static T[] Copy<T>(T[] a) {
        T[] ret = new T[a.Length];
        Array.Copy(a, ret, a.Length);
        return ret;
    }

    internal static MapMetaMountainCamera Copy(MapMetaMountainCamera camera) {
        return new MapMetaMountainCamera {
            Position = camera.Position,
            Target = camera.Target
        };
    }*/
}