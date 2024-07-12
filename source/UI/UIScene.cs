﻿using System;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Snowberry.UI;

public abstract class UIScene : Scene {

    public static readonly MTexture CursorsAtlas = GFX.Gui["Snowberry/cursors"];
    public static readonly MTexture ActionbarAtlas = GFX.Gui["Snowberry/actionbar_icons"];
    public static readonly Color BG = Calc.HexToColor("060607");
    public static readonly MTexture DefaultCursor = CursorsAtlas.GetSubtexture(0, 0, 16, 16);

    public static bool DebugShowUIBounds = false;

    public static int UiScale => Snowberry.Settings.SmallScale ? 1 : 2;

    public RenderTarget2D UIBuffer;
    public UIElement UI = new();
    public UIMessage Message;
    public UIElement Overlay;
    public bool MouseClicked = false;

    public static UIScene Instance => Engine.Scene as UIScene;

    public override void Begin() {
        base.Begin();

        UIBuffer = new RenderTarget2D(Engine.Instance.GraphicsDevice, Engine.ViewWidth / UiScale, Engine.ViewHeight / UiScale);
        UI.Width = UIBuffer.Width;
        UI.Height = UIBuffer.Height;

        BeginContent();

        UI.Add(Message = new UIMessage {
            Width = UI.Width,
            Height = UI.Height
        });

        UI.Add(Overlay = new UIElement {
            Width = UI.Width,
            Height = UI.Height
        });

        PostBeginContent();
    }

    public override void End() {
        base.End();
        UIBuffer.Dispose();
        UI.Destroy();
    }

    public override void Update() {
        base.Update();

        if (Engine.ViewWidth / UiScale != UI.Width || Engine.ViewHeight / UiScale != UI.Height) {
            UIBuffer.Dispose();
            UIBuffer = new RenderTarget2D(Engine.Instance.GraphicsDevice, Engine.ViewWidth / UiScale, Engine.ViewHeight / UiScale);
            UI.Width = UIBuffer.Width;
            UI.Height = UIBuffer.Height;
            Message.Width = UIBuffer.Width;
            Message.Height = UIBuffer.Height;
            Overlay.Width = UIBuffer.Width;
            Overlay.Height = UIBuffer.Height;
            OnScreenResized();
        }

        Mouse.WorldLast = Mouse.World;
        Mouse.ScreenLast = Mouse.Screen;

        MouseState m = Microsoft.Xna.Framework.Input.Mouse.GetState();
        Mouse.Screen = new Vector2(m.X, m.Y) / UiScale;
        Mouse.World = CalculateMouseWorld(m);

        MouseClicked = false;
        UI.Update();

        UpdateContent();

        if (MInput.Mouse.PressedLeftButton)
            Mouse.LastClick = DateTime.Now;
    }

    public override void Render() {
        Draw.SpriteBatch.GraphicsDevice.Clear(BG);

        bool showUi = ShouldShowUi();

        #region UI Rendering

        Engine.Instance.GraphicsDevice.SetRenderTarget(UIBuffer);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

        if(showUi)
            UI.Render();

        MTexture curCursor = DefaultCursor;
        Vector2 curJustify = Vector2.Zero;
        SuggestCursor(ref curCursor, ref curJustify);
        curCursor.DrawJustified(Mouse.Screen.Floor(), curJustify, Color.White * (showUi ? 1 : 0.5f));

        // Tooltip rendering
        var tooltip = UI.HoveredTooltip();
        if (tooltip != null) {
            string[] array = tooltip.Split(["\\n"], StringSplitOptions.None);
            for(int i = 0; i < array.Length; i++) {
                string line = array[i];
                var tooltipArea = Fonts.Regular.Measure(line);
                var at = Mouse.Screen.Floor() - new Vector2(tooltipArea.X + 8, -(tooltipArea.Y + 6) * i);
                Draw.Rect(at, tooltipArea.X + 8, tooltipArea.Y + 6, Color.Black * 0.8f);
                Fonts.Regular.Draw(line, at + new Vector2(4, 3), Vector2.One, Color.White);
            }
        }

        Draw.SpriteBatch.End();

        #endregion

        RenderContent();

        #region Displaying on Backbuffer

        Engine.Instance.GraphicsDevice.SetRenderTarget(null);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        Draw.SpriteBatch.Draw(UIBuffer, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One * UiScale, SpriteEffects.None, 0f);
        Draw.SpriteBatch.End();

        #endregion
    }

    protected virtual void BeginContent() {}
    protected virtual void PostBeginContent() {}
    protected virtual void RenderContent() {}
    protected virtual void UpdateContent() {}
    protected virtual Vector2 CalculateMouseWorld(MouseState m) => new Vector2(m.X, m.Y) / UiScale;
    protected virtual void SuggestCursor(ref MTexture texture, ref Vector2 justify) {}
    protected virtual void OnScreenResized() {}
    protected virtual bool ShouldShowUi() => true;
}