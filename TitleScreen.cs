using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class TitleScreen
{
    private static readonly RenderTarget2D TitleTarget = new(Main.Graphics.GraphicsDevice, Ui.UiWidth, Ui.UiHeight);
    private static Texture2D _title;

    internal static void LoadContent()
    {
        _title = Main.ContentManager.Load<Texture2D>("images/title/title");
    }

    internal static void Update(KeyboardState keyboardState)
    {
        if (keyboardState.IsKeyDown(Keys.Enter))
        {
            Console.WriteLine("aa");
            StateMachine.ChangeState(StateMachine.Action.LoadMap);
        }
    }

    private static void DrawTitleScreen()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(TitleTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Crimson);
        
        Main.SpriteBatch.Begin();
        Main.SpriteBatch.Draw(_title, Vector2.Zero, Color.White);
        Main.SpriteBatch.End();
    }

    internal static void Draw()
    {
        DrawTitleScreen();
        
        Main.Graphics.GraphicsDevice.SetRenderTarget(WindowManager.Scene);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Main.SpriteBatch.Draw(TitleTarget, WindowManager.Scene.Bounds, Color.White);
        Main.SpriteBatch.End();
    }
}