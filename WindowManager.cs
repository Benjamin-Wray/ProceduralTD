using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class WindowManager
{
    internal static RenderTarget2D Scene { get; private set; } //the whole game will be drawn to this render target so it can be scaled to any resolution
    
    private static Texture2D _cursor; //mouse cursor texture

    private static bool _f11Down; //boolean for keeping track of if the f11 key is currently down

    //window dimensions
    private const int DefaultWindowWidth = 1280;
    private const int DefaultWindowHeight = 720;
    private static int _currentWidth = DefaultWindowWidth;
    private static int _currentHeight = DefaultWindowHeight;
    private static int _windowedWidth;
    private static int _windowedHeight;

    //scene dimensions
    private const int SceneWidth = 512;
    private const int SceneHeight = 288;
    private static Rectangle _sceneSize;

    internal static void Initialize()
    {
        Scene = new RenderTarget2D(Main.Graphics.GraphicsDevice, SceneWidth, SceneHeight); //create scene render target

        SetWindowSize(); //set size of window
        SetSceneSize(); //set size and position of scene
        
        Main.GameWindow.AllowUserResizing = true; //enable window resizing
        Main.GameWindow.ClientSizeChanged += OnResize;
    }

    internal static void LoadContent()
    {
        _cursor = Main.ContentManager.Load<Texture2D>("images/cursor");
    }

    //called whenever the game window is resized
    private static void OnResize(object? sender, EventArgs eventArgs)
    {
        //update the current dimensions of the window
        _currentWidth = Main.Graphics.GraphicsDevice.Viewport.Width;
        _currentHeight = Main.Graphics.GraphicsDevice.Viewport.Height;
        
        //clamp minimum window size to size of scene
        if (Main.Graphics.GraphicsDevice.Viewport.Width < SceneWidth) Main.Graphics.PreferredBackBufferWidth = SceneWidth;
        if (Main.Graphics.GraphicsDevice.Viewport.Height < SceneHeight) Main.Graphics.PreferredBackBufferHeight = SceneHeight;
        Main.Graphics.ApplyChanges();
        
        SetSceneSize(); //update the size and position of the scene render target to fit new window size
    }

    internal static void Update()
    {
        KeyboardState keyboardState = Keyboard.GetState();
        
        if (keyboardState.IsKeyDown(Keys.Escape)) Main.ExitGame(); //exits the game if the escape key is pressed
        
        //toggles fullscreen when F11 is pressed
        if (keyboardState.IsKeyDown(Keys.F11) && !_f11Down)
        {
            _f11Down = true; //will not execute again until F11 key is released
            FullScreenToggle();
        }
        else if (keyboardState.IsKeyUp(Keys.F11)) _f11Down = false;
    }

    internal static Point GetMouseInRectangle(Point mousePosition, Rectangle renderTargetBounds)
    {
        //normalises the mouse position between the scene bounds and multiplies it by the dimensions of the render target
        mousePosition.X = (int)(MapGenerator.Normalize(mousePosition.X, _sceneSize.Left, _sceneSize.Right) * renderTargetBounds.Width);
        mousePosition.Y = (int)(MapGenerator.Normalize(mousePosition.Y, _sceneSize.Top, _sceneSize.Bottom) * renderTargetBounds.Height);
        
        return mousePosition;
    }
    
    //draws the mouse to the screen
    private static void DrawMouse()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(Scene);

        Vector2 newMousePosition = GetMouseInRectangle(Mouse.GetState().Position, Scene.Bounds).ToVector2(); //get the position of the mouse on the render target
        
        Main.SpriteBatch.Begin();
        Main.SpriteBatch.Draw(_cursor, newMousePosition, Color.White); //draw the mouse to the render target
        Ui.DrawPrice(newMousePosition, _cursor);
        Main.SpriteBatch.End();
    }
    
    internal static void Draw()
    {
        DrawMouse(); //the mouse will be the last thing to be drawn so it always appears on top
        
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