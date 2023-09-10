using System;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Snowberry.Editor.Entities;
using Snowberry.UI;
using Snowberry.UI.Menus;
using Snowberry.UI.Menus.MainMenu;

namespace Snowberry.Editor;

public class Editor : UIScene {
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

    public static new Editor Instance { get; private set; }

    // just forward to settings
    // don't expose setter here to make it explicit that changing this == changing user settings
    public static bool FancyRender => Snowberry.Settings.FancyRender;
    public static bool StylegroundsPreviews => Snowberry.Settings.StylegroundsPreview;

    public static readonly MTexture panningCursor = CursorsAtlas.GetSubtexture(32, 16, 16, 16);

    public BufferCamera Camera { get; private set; }

    public Vector2 mousePos, lastMousePos;
    public Vector2 worldClick;

    public Map Map { get; private set; }

    internal static Rectangle? SelectionInProgress;
    internal static Room SelectedRoom;
    internal static int SelectedFillerIndex = -1;
    internal static List<Selection> SelectedObjects;

    public UIToolbar Toolbar;
    public UIElement ToolPanel;
    public UIElement ActionBar, ToolActionGroup;

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

    internal static void OpenFancy(MapData data) {
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
        Audio.Stop(Audio.CurrentMusicEventInstance);
        Map map = null;
        if (data != null) {
            Snowberry.Log(LogLevel.Info, $"Opening level editor using map {data.Area.GetSID()}");
            map = new Map(data);
        }

        var _ = new FadeWipe(Engine.Scene, false, () => {
            Engine.Scene = new Editor(map);
        }) {
            Duration = data != null ? 0.3f : 0.85f
        };
    }

    private void MenuUI() {
        UI.Add(new UIMainMenu(UIBuffer.Width, UIBuffer.Height));
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
                AreaMode.Normal => "A-Side",
                AreaMode.BSide => "B-Side",
                AreaMode.CSide => "C-Side",
                _ => "???"
            }) {
                Underline = true,
                FG = Color.Gold
            }, new Vector2(10, 11));
        }

        UIButton play = new UIKeyboundButton(ActionbarAtlas.GetSubtexture(0, 0, 16, 16), 3, 3) {
            OnPress = () => {
                Audio.SetMusic(null);
                Audio.SetAmbience(null);

                SaveData.InitializeDebugMode();

                generatePlaytestMapData = true;
                PlaytestMapData = new MapData(Map.From);
                PlaytestSession = new Session(Map.From);
                if (SelectedRoom != null) {
                    PlaytestSession.RespawnPoint = SelectedRoom.Entities.OfType<Plugin_Player>().FirstOrDefault()?.Position;
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
            },
            Ctrl = true,
            Key = Keys.P
        };
        ActionBar.AddRight(play, new(10, 4));

        UIButton save = new UIKeyboundButton(ActionbarAtlas.GetSubtexture(16, 0, 16, 16), 3, 3) {
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
                    info.Position -= offset;
                    newName.Position -= offset;
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
                    if (From != null)
                        AssetReloadHelper.Do(Dialog.Clean("ASSETRELOADHELPER_RELOADINGMAP"), () => AreaData.Areas[From.Value.ID].Mode[0].MapData.Reload());
                }
            },
            Ctrl = true,
            Key = Keys.S
        };
        ActionBar.AddRight(save, new(6, 4));

        UIButton exit = new UIKeyboundButton(ActionbarAtlas.GetSubtexture(32, 0, 16, 16), 3, 3) {
            OnPress = () => {
                // TODO: show an "are you sure" message
                Engine.Scene = new OverworldLoader(Overworld.StartMode.MainMenu);
            },
            Ctrl = true,
            Alt = true,
            Key = Keys.Q
        };
        ActionBar.AddRight(exit, new(6, 4));

        UI.AddBelow(new UILabel(() => $"Room: {SelectedRoom?.Name ?? (SelectedFillerIndex > -1 ? $"(filler: {SelectedFillerIndex})" : "(none)")}"), new(10));

        SwitchTool(0);
    }

    protected override void BeginContent() {
        Camera = new BufferCamera();

        if (Map == null)
            MenuUI();
        else
            MappingUI();
    }

    public override void End() {
        base.End();
        Camera.Buffer?.Dispose();
    }

    protected override void UpdateContent() {
        lastMousePos = mousePos;
        mousePos = MInput.Mouse.Position;

        // zooming
        bool canZoom = UI.CanScrollThrough();
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
        bool canClick = UI.CanClickThrough() && !Message.Shown;

        // panning
        bool middlePan = Snowberry.Settings.MiddleClickPan;
        if ((middlePan && MInput.Mouse.CheckMiddleButton || !middlePan && MInput.Mouse.CheckRightButton) && canClick) {
            Vector2 move = lastMousePos - mousePos;
            if (move != Vector2.Zero)
                Camera.Position += move / (Camera.Buffer == null ? Camera.Zoom : 1f);
        }

        if (Map != null) {
            // room & filler select
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
            if (CanTypeShortcut() && (MInput.Keyboard.Check(Keys.LeftControl, Keys.RightControl))) {
                bool save = false;
                if (MInput.Keyboard.Pressed(Keys.F)) {
                    Snowberry.Settings.FancyRender = !Snowberry.Settings.FancyRender;
                    save = true;
                }

                if (MInput.Keyboard.Pressed(Keys.L)) {
                    Snowberry.Settings.StylegroundsPreview = !Snowberry.Settings.StylegroundsPreview;
                    save = true;
                }

                if (save)
                    Snowberry.Instance.SaveSettings();
            }
        }
    }

    protected override Vector2 CalculateMouseWorld(MouseState m) {
        return Vector2.Transform(Camera.Buffer == null ? new(m.X, m.Y) : mousePos, Camera.Inverse).Floor();
    }

    public void SwitchTool(int toolIdx) {
        ToolPanel?.RemoveSelf();

        Toolbar.CurrentTool = toolIdx;
        var tool = Tool.Tools[toolIdx];
        ToolPanel = tool.CreatePanel(UIBuffer.Height - Toolbar.Height);
        ToolPanel.Position = new Vector2(UIBuffer.Width - ToolPanel.Width, Toolbar.Height);
        UI.Add(ToolPanel);

        ToolActionGroup?.RemoveSelf();
        ToolActionGroup = null;
        ActionBar.Update(); // get rid of the old action group immediately
        ActionBar.Width = UIBuffer.Width - ToolPanel.Width;
        var toolActionGroup = tool.CreateActionBar();
        if (toolActionGroup != null) {
            toolActionGroup.Position = new(5, 0);
            toolActionGroup.CalculateBounds(); // required for sub-elements to get any tooltips
            ToolActionGroup = new();
            ToolActionGroup.AddRight(new UILabel("|") {
                Position = new(5, 11)
            });
            ToolActionGroup.AddRight(toolActionGroup);
            ToolActionGroup.CalculateBounds(); // same
            ActionBar.AddRight(ToolActionGroup);
        }

        SelectedObjects = null;
    }

    protected override void RenderContent() {
        var tool = Map == null ? null : Tool.Tools[Toolbar.CurrentTool];

        #region Tool Rendering

        if (tool != null) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            tool.RenderScreenSpace();
            Draw.SpriteBatch.End();
        }

        #endregion

        #region Map Rendering

        Engine.Instance.GraphicsDevice.SetRenderTarget(Camera.Buffer);

        Engine.Instance.GraphicsDevice.Clear(BG);
        if (Map != null) {
            Map.Render(Camera);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Camera.Matrix);
            tool.RenderWorldSpace();
            Draw.SpriteBatch.End();
            Map.PostRender();
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

        #endregion
    }

    protected override void SuggestCursor(ref MTexture texture, ref Vector2 justify) {
        var tool = Map == null ? null : Tool.Tools[Toolbar.CurrentTool];
        bool canClick = UI.CanClickThrough() && !Message.Shown;
        bool middlePan = Snowberry.Settings.MiddleClickPan;
        var panning = (middlePan && MInput.Mouse.CheckMiddleButton || !middlePan && MInput.Mouse.CheckRightButton) && canClick;

        if (panning) {
            texture = panningCursor;
            justify = new(0.5f);
        } else if (canClick)
            tool?.SuggestCursor(ref texture, ref justify);
    }

    public bool CanTypeShortcut() => !UI.NestedGrabsKeyboard();

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

    internal static void CopyMapMeta(MapMeta from, MapMeta to) {
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

        if (from.CassetteModifier != null)
            to.CassetteModifier = new MapMetaCassetteModifier {
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