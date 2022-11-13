using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class WindowManager
{
    internal static RenderTarget2D Scene { get; private set; } //the whole game will be drawn to this render target so it can be scaled to any resolution

    private static Texture2D? _cursor; //mouse cursor texture

    private static bool _f11Down; //boolean for keeping track of if the f11 key is currently down

    //window dimensions
    private static int _currentWidth;
    private static int _currentHeight;
    private static int _windowedWidth;
    private static int _windowedHeight;

    //scene dimensions
    private const int SceneWidth = 1920;
    private const int SceneHeight = 1080;
    private static Rectangle _sceneSize;
    
    //mouse
    private static Vector2 _mousePosition;
    private static Texture2D? _cursorTexture;
    private static Color _mouseColour;
    private const float MouseScale = SceneHeight / (float)Ui.UiHeight;

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
        SetSceneSize(); //update the size and position of the scene render target to fit new window size
    }

    internal static void Update()
    {
        KeyboardState keyboardState = Keyboard.GetState();
        
        if (keyboardState.IsKeyDown(Keys.Escape)) Program.Game.Exit(); //exits the game if the escape key is pressed
        
        //toggles fullscreen when F11 is pressed
        if (keyboardState.IsKeyDown(Keys.F11) && !_f11Down)
        {
            _f11Down = true; //will not execute again until F11 key is released
            FullScreenToggle();
        }
        else if (keyboardState.IsKeyUp(Keys.F11)) _f11Down = false;
        
        _mousePosition = GetMouseInRectangle(Scene.Bounds).ToVector2(); //get the position of the mouse on the render target
        _cursorTexture = _cursor; //sets the texture to be drawn as the cursor
        _mouseColour = Color.White; //sets the initial colour to white
        if (Camera.CameraTarget.Bounds.Contains(GetMouseInRectangle(Scene.Bounds)))
        {
            if (Ui.SelectedOption is (int)TowerManager.MenuOptions.Upgrade or (int)TowerManager.MenuOptions.Sell) //if upgrade or sell is selected
            {
                _cursorTexture = Ui.ButtonDrawOrder[Ui.SelectedOption.Value];
                _mousePosition = Ui.CentrePosition(_mousePosition, _cursorTexture, MouseScale);
                _mouseColour.A = (byte)(_mouseColour.A * .8f);
            }
            else if (TowerManager.SelectedTower != null) _cursorTexture = null;
        }
    }

    internal static Point GetMouseInRectangle(Rectangle renderTargetBounds)
    {
        Point mousePosition = Mouse.GetState().Position;
        //normalises the mouse position between the scene bounds and multiplies it by the dimensions of the render target
        mousePosition.X = (int)(MapGenerator.Normalize(mousePosition.X, _sceneSize.Left, _sceneSize.Right) * renderTargetBounds.Width);
        mousePosition.Y = (int)(MapGenerator.Normalize(mousePosition.Y, _sceneSize.Top, _sceneSize.Bottom) * renderTargetBounds.Height);
        
        return mousePosition;
    }
    
    //draws the mouse to the screen
    private static void DrawMouse()
    {
        if (_cursorTexture == null) return;
        
        Main.Graphics.GraphicsDevice.SetRenderTarget(Scene);
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        Main.SpriteBatch.Draw(_cursorTexture, _mousePosition, null, _mouseColour, 0f, Vector2.Zero, MouseScale, SpriteEffects.None, 0f); //draw the mouse to the render target
        Ui.DrawCursorPrice(_mousePosition, _cursorTexture, MouseScale);
        
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
        int defaultWindowWidth = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width * 3/4f);
        int defaultWindowHeight = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height * 3/4f);
        _currentWidth = defaultWindowWidth;
        _currentHeight = defaultWindowHeight;
        _windowedWidth = defaultWindowWidth;
        _windowedHeight = defaultWindowHeight;
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
        //update current window width and height values
        _currentWidth = Main.Graphics.GraphicsDevice.Viewport.Width;
        _currentHeight = Main.Graphics.GraphicsDevice.Viewport.Height;
        
        //set initial dimensions to the same as the window
        _sceneSize = new Rectangle(0, 0, _currentWidth, _currentHeight);

        //aspect ratios of window and scene render targets
        float windowAspect = _currentWidth / (float) _currentHeight;
        float sceneAspect = SceneWidth / (float) SceneHeight;
        
        if (windowAspect > sceneAspect) //if window aspect ratio is wider than scene aspect ratio
        {
            //adjust the width of the scene so it has the current aspect ratio and position the scene at the centre of the window
            _sceneSize.Width = SceneWidth * _currentHeight / SceneHeight;
            _sceneSize.X = (_currentWidth - _sceneSize.Width) / 2;
        }
        else if (windowAspect < sceneAspect) //if window aspect ratio is taller than scene aspect ratio
        {
            //adjust the height of the scene so it has the correct aspect ratio and position the scene at the centre of the window
            _sceneSize.Height = SceneHeight * _currentWidth / SceneWidth;
            _sceneSize.Y = (_currentHeight - _sceneSize.Height) / 2;
        }
    }
}