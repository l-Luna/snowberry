using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Monocle;
using Snowberry.Editor;
using Snowberry.Editor.LoennInterop;
using Snowberry.Editor.Placements;
using Snowberry.Editor.Recording;

namespace Snowberry;

public sealed class Snowberry : EverestModule {

    public const string PlaytestSid = "Snowberry/Playtest";

    public static Snowberry Instance {
        get;
        private set;
    }

    public static SnowberryModule[] Modules { get; private set; }

    public Snowberry() {
        Instance = this;
    }

    public override Type SettingsType => typeof(SnowberrySettings);
    public static SnowberrySettings Settings => (SnowberrySettings)Instance._Settings;

    public override void Load() {
        On.Celeste.MapData.StartLevel += DontCrashOnEmptyPlaytestLevel;
        On.Celeste.LevelEnter.Routine += DontEnterPlaytestMap;
        On.Celeste.AnimatedTiles.Set += DontCrashOnMissingAnimatedTile;
        On.Celeste.BinaryPacker.FromBinary += UsePlaytestMapBinary;

        Everest.Events.MainMenu.OnCreateButtons += MainMenu_OnCreateButtons;
        Everest.Events.Level.OnCreatePauseMenuButtons += Level_OnCreatePauseMenuButtons;

        RecInProgress.Load();
    }

    public override void LoadContent(bool firstLoad) {
        base.LoadContent(firstLoad);

        LoadModules();
        LoennPluginLoader.LoadPlugins();

        Fonts.Load();
        DecalPlacementProvider.Reload();
    }

    public override void Unload() {
        On.Celeste.MapData.StartLevel -= DontCrashOnEmptyPlaytestLevel;
        On.Celeste.LevelEnter.Routine -= DontEnterPlaytestMap;
        On.Celeste.AnimatedTiles.Set -= DontCrashOnMissingAnimatedTile;
        On.Celeste.BinaryPacker.FromBinary -= UsePlaytestMapBinary;

        Everest.Events.MainMenu.OnCreateButtons -= MainMenu_OnCreateButtons;
        Everest.Events.Level.OnCreatePauseMenuButtons -= Level_OnCreatePauseMenuButtons;

        RecInProgress.Unload();
    }

    private static void LoadModules() {
        List<SnowberryModule> modules = [];

        foreach (EverestModule module in Everest.Modules) {
            Assembly asm = module.GetType().Assembly;
            var types = asm.GetTypesSafe();
            SnowberryModule sm = null;

            foreach (Type type in types.Where(t => !t.IsAbstract && typeof(SnowberryModule).IsAssignableFrom(t))) {
                if (sm != null) {
                    Log(LogLevel.Warn, $"Mod '{module.Metadata.Name}' contains extra Snowberry module at '{type.FullName}' that will be ignored!");
                    continue;
                }

                ConstructorInfo ctor = type.GetConstructor(Array.Empty<Type>());
                if (ctor != null) {
                    sm = (SnowberryModule)ctor.Invoke(Array.Empty<object>());
                    PluginInfo.GenerateFromAssembly(asm, sm);
                    modules.Add(sm);
                    Log(LogLevel.Info, $"Successfully loaded Snowberry module '{sm.Name}' from '{module.Metadata.Name}'");
                }
            }

            if (sm != null && module.GetType() != typeof(Snowberry))
                foreach (Type type in types.Where(t => !t.IsAbstract && typeof(Tool).IsAssignableFrom(t)))
                    if (type.GetConstructor(Array.Empty<Type>()) is {} ctor) {
                        Tool pluginTool = (Tool)ctor.Invoke(Array.Empty<object>());
                        pluginTool.Owner = sm;
                        Tool.Tools.Add(pluginTool);
                        Log(LogLevel.Info, $"Loaded plugin tool '{pluginTool.GetName()}' from {module.Metadata.Name}");
                    }
        }

        Modules = modules.ToArray();
    }

    private static void MainMenu_OnCreateButtons(OuiMainMenu menu, List<MenuButton> buttons) {
        MainMenuSmallButton btn = new MainMenuSmallButton("EDITOR_MAINMENU", "menu/editor", menu, Vector2.Zero, Vector2.Zero, () => MainMenu.OpenMainMenu());
        int idx = 2;
        if (Celeste.Celeste.PlayMode == Celeste.Celeste.PlayModes.Debug)
            idx++;
        buttons.Insert(idx, btn);
    }

    // from Collab Utils 2, adjusted for Snowberry
    private static void Level_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal) {
        int buttonIdx(string label) =>
            menu.Items.FindIndex(item =>
                item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean(label));

        if (level.Session == Editor.Editor.PlaytestSession) {
            // find the position just under "Return to Map".
            var returnToMapIndex = buttonIdx("MENU_PAUSE_RETURN");

            // TODO: uncomment once Playtest is inaccessible through level select
            /*if (returnToMapIndex == -1) {
                // fall back to the bottom of the menu.
                returnToMapIndex = menu.GetItems().Count - 1;
            }*/

            if (returnToMapIndex != -1) {
                // instantiate the "Return to Editor" button
                TextMenu.Button rteBtn = new TextMenu.Button(Dialog.Clean("SNOWBERRY_RETURN_TO_EDITOR"));
                rteBtn.Pressed(() => Editor.Editor.Open(level.Session.MapData, rte: true));

                // replace the "Return to Map" button with "Return to Editor"
                menu.Remove(menu.Items[returnToMapIndex]);
                menu.Insert(returnToMapIndex, rteBtn);
            }

            // find the position just under "Save and Quit".
            int saveAndQuitIndex = buttonIdx("MENU_PAUSE_SAVEQUIT");

            // TODO: uncomment once Playtest is inaccessible through level select
            /*if (saveAndQuitIndex == -1) {
                // fall back to the bottom of the menu.
                saveAndQuitIndex = menu.GetItems().Count - 1;
            }*/

            if (saveAndQuitIndex != -1) {
                // TODO: add confirmation screen w/ "save and quit", "quit without saving", and "cancel"

                // instantiate the "Quit" button
                TextMenu.Button quitBtn = new TextMenu.Button(Dialog.Clean("SNOWBERRY_QUIT"));
                quitBtn.Pressed(() => level.DoScreenWipe(false, () => Engine.Scene = new LevelExit(LevelExit.Mode.SaveAndQuit, level.Session, level.HiresSnow), true));
                // quitBtn.ConfirmSfx = "event:/ui/main/message_confirm";

                // replace the "Save and Quit" button with "Quit"
                menu.Remove(menu.Items[saveAndQuitIndex]);
                menu.Insert(saveAndQuitIndex, quitBtn);
            }
        }
    }

    public static void Log(LogLevel level, string message) => Logger.Log(level, "Snowberry", message);

    public static void LogInfo(string message) => Log(LogLevel.Info, message);

    private static LevelData DontCrashOnEmptyPlaytestLevel(On.Celeste.MapData.orig_StartLevel orig, MapData self) {
        // TODO: just add an empty room lol
        if (self.Area.SID == PlaytestSid && self.Levels.Count == 0) {
            var empty = new BinaryPacker.Element {
                Children = [],
                Attributes = new() {
                    ["name"] = "lvl_empty_map"
                }
            };
            return new LevelData(empty);
        }

        return orig(self);
    }

    private static System.Collections.IEnumerator DontEnterPlaytestMap(On.Celeste.LevelEnter.orig_Routine orig, LevelEnter self) {
        var session = new DynamicData(self).Get<Session>("session");
        if (session.Area.SID == PlaytestSid && session != Editor.Editor.PlaytestSession && string.IsNullOrEmpty(LevelEnter.ErrorMessage)) {
            return CantEnterRoutine(self);
        }

        return orig(self);
    }

    private static System.Collections.IEnumerator CantEnterRoutine(LevelEnter self) {
        yield return 1f;
        Postcard postcard;
        self.Add(postcard = new Postcard(Dialog.Get("SNOWBERRY_PLAYTEST_MAP_POSTCARD"), "event:/ui/main/postcard_csides_in", "event:/ui/main/postcard_csides_out"));
        new DynamicData(self).Set("postcard", postcard);
        yield return postcard.DisplayRoutine();
        SaveData.Instance.CurrentSession_Safe = new Session(AreaKey.Default);
        SaveData.Instance.LastArea_Safe = AreaKey.Default;

        Engine.Scene = new OverworldLoader(Overworld.StartMode.AreaQuit);
    }

    private static void DontCrashOnMissingAnimatedTile(On.Celeste.AnimatedTiles.orig_Set orig, AnimatedTiles self, int x, int y, string name, float scalex, float scaley) {
        if (self.Bank.AnimationsByName.ContainsKey(name))
            orig(self, x, y, name, scalex, scaley);
    }

    private static BinaryPacker.Element UsePlaytestMapBinary(On.Celeste.BinaryPacker.orig_FromBinary orig, string filename) {
        if (Editor.Editor.generatePlaytestMapData && Engine.Scene is Editor.Editor editor)
            return editor.Map.Export();
        return orig(filename);
    }
}