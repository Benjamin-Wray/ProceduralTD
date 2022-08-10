using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class WindowManager
{
    internal static RenderTarget2D Scene; //the whole game will be drawn to this render target so it can be scaled to any resolution

    //boolean for keeping track of if the f11 key is currently down
    private static bool _f11Down;

    //window dimensions
    private const int DefaultWindowWidth = 1280;
    private const int DefaultWindowHeight = 720;
    private static int _currentWidth = DefaultWindowWidth;
    private static int _currentHeight = DefaultWindowHeight;
    private static int _windowedWidth;
    private static int _windowedHeight;

    //scene dimensions
    private const int SceneWidth = Ui.UiWidth * 2;
    private const int SceneHeight = Ui.UiHeight * 2;
    private static Rectangle _sceneSize;

    internal static void Initialize(GameWindow window)
    {
        Scene = new RenderTarget2D(Main.Graphics.GraphicsDevice, SceneWidth, SceneHeight); //create scene render target

        SetWindowSize(); //set size of window
        SetSceneSize(); //set size and position of scene
        
        window.AllowUserResizing = true; //enable window resizing
        window.ClientSizeChanged += OnResize;
    }

    //called whenever the game window is resized
    private static void OnResize(object? sender, EventArgs eventArgs)
    {
        //update the current dimensions of the window
        _currentWidth = Main.Graphics.GraphicsDevice.Viewport.Width;
        _currentHeight = Main.Graphics.GraphicsDevice.Viewport.Height;
        
        //clamp minimum window size to size of scene
        if (_currentWidth < SceneWidth) Main.Graphics.PreferredBackBufferWidth = SceneWidth;
        if (_currentHeight < SceneHeight) Main.Graphics.PreferredBackBufferHeight = SceneHeight;
        Main.Graphics.ApplyChanges();
        
        SetSceneSize(); //update the size and position of the scene render target to fit new window size
    }
    
    internal static void Update()
    {
        //toggles fullscreen when F11 is pressed
        if (Main.KeyState.IsKeyDown(Keys.F11) && _f11Down == false)
        {
            _f11Down = true; //will not execute again until F11 key is released
            FullScreenToggle();
        }
        else if (Main.KeyState.IsKeyUp(Keys.F11) && _f11Down) _f11Down = false;
    }

    internal static Point GetMouseInRectangle(Rectangle renderTargetBounds)
    {
        Point mousePosition = Main.MouseState.Position;
        mousePosition.X = (int)((mousePosition.X - renderTargetBounds.X) / (float)(_currentWidth - renderTargetBounds.X * 2) * renderTargetBounds.Width);
        mousePosition.Y = (int)((mousePosition.Y - renderTargetBounds.Y) / (float)(_currentHeight - renderTargetBounds.Y * 2) * renderTargetBounds.Height);
        return mousePosition;
    }
    
    internal static void Draw()
    {
        Ui.Draw(); //draw the game

        Main.Graphics.GraphicsDevice.SetRenderTarget(null); //start drawing to window
        Main.Graphics.GraphicsDevice.Clear(Color.Black); //set the colour of bars seen in letterboxing when window and scene aspect ratios don't match
        
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp); //start drawing in point clamp mode because we want to keep the hard edges of the pixel art
        Main.SpriteBatch.Draw(Scene, _sceneSize, Color.White); //draw the scene to the window
        Main.SpriteBatch.End();
        
    }
    
    private static void SetWindowSize() //set initial window dimensions
    {
        _windowedWidth = DefaultWindowWidth;
        _windowedHeight = DefaultWindowHeight;
        Main.Graphics.PreferredBackBufferWidth = _windowedWidth;
        Main.Graphics.PreferredBackBufferHeight = _windowedHeight;
        Main.Graphics.ApplyChanges();
    }
    
    private static void FullScreenToggle() //toggles fullscreen
    {
        if (Main.Graphics.IsFullScreen) //program is currently in fullscreen
        {
            //set window dimensions back to original values
            Main.Graphics.PreferredBackBufferWidth = _windowedWidth;
            Main.Graphics.PreferredBackBufferHeight = _windowedHeight;
        }
        else
        {
            //store window dimensions so they can be restored if fullscreen is disabled
            _windowedWidth = Main.Graphics.GraphicsDevice.Viewport.Width;
            _windowedHeight = Main.Graphics.GraphicsDevice.Viewport.Height;
                
            //set window resolution to match display resolution
            Main.Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            Main.Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            Main.Graphics.ApplyChanges();
        }
        Main.Graphics.ToggleFullScreen();
    }
    
    private static void SetSceneSize() //set size of scene render target
    {
        _currentWidth = Main.Graphics.GraphicsDevice.Viewport.Width;
        _currentHeight = Main.Graphics.GraphicsDevice.Viewport.Height;
        
        //set initial dimensions to the same as the window
        Point rectangleSize = new Point(_currentWidth, _currentHeight);
        Point rectangleOffset = Point.Zero;

        if (_currentWidth / (float)_currentHeight > SceneWidth / (float)SceneHeight) //if window aspect ratio is wider than scene aspect ratio
        {
            //adjust the width of the scene so it has the current aspect ratio and position the scene at the centre of the window
            rectangleSize.X = SceneWidth * _currentHeight / SceneHeight;
            rectangleOffset.X = (_currentWidth - rectangleSize.X) / 2;
        }
        else //if window aspect ratio is taller than scene aspect ratio
        {
            //adjust the height of the scene so it has the correct aspect ratio and position the scene at the centre of the window
            rectangleSize.Y = SceneHeight * _currentWidth / SceneWidth;
            rectangleOffset.Y = (_currentHeight - rectangleSize.Y) / 2;
        }
        
        _sceneSize = new Rectangle(rectangleOffset, rectangleSize); //update the size of the scene with calculated values
    }
}