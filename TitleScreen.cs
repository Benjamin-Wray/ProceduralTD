using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class TitleScreen
{
    private static readonly RenderTarget2D TitleTarget = new(Main.Graphics.GraphicsDevice, Ui.HudWidth / 2, Ui.HudHeight / 2);
    
    //textures
    private static Texture2D _title;
    private static Texture2D _seedBox;
    private static Texture2D[] _startButtonFrames;
    private static int _startButtonIndex;
    private static Texture2D[] _loadingFrames;
    private static int _loadingIndex;

    //colours
    private static readonly Color BackgroundColour = new(135, 174, 142);
    private static readonly Color TitleColour = new(58, 69, 104);
    private static readonly Color TextColour = Color.Black;

    //used for calculating position of textures to be drawn
    private const int TitleOffset = 4; //distance of title from the top of the window
    private const int SeedOffset = 8; //distance of the seed input box from the title
    private const int SeedStartSpace = 4; //space between the seed box and the start button


    //the positions of each texture on the screen
    private static Vector2 _titlePosition;
    private static Vector2 _seedBoxPosition;
    private static Vector2 _seedPosition;
    private static Rectangle _startRectangle; //the start button uses a rectangle so the program can check if the mouse is hovering over it
    private static Vector2 _loadingPosition;

    private static bool _isStartButtonDown; //tells the program if the start button is currently down

    private static string _seed = ""; //stores the seed used for map generation
    private const int MaxSeedLength = 8; //the maximum number of digits for the seed


    //the keys that can be pressed in order to input numbers into the seed input box

    private static readonly Dictionary<Keys, char> NumberKeys = new()
    {
        {Keys.D0, '0'}, {Keys.D1, '1'}, {Keys.D2, '2'}, {Keys.D3, '3'}, {Keys.D4, '4'}, {Keys.D5, '5'}, {Keys.D6, '6'}, {Keys.D7, '7'}, {Keys.D8, '8'}, {Keys.D9, '9'},
        {Keys.NumPad0, '0'}, {Keys.NumPad1, '1'}, {Keys.NumPad2, '2'}, {Keys.NumPad3, '3'}, {Keys.NumPad4, '4'}, {Keys.NumPad5, '5'}, {Keys.NumPad6, '6'}, {Keys.NumPad7, '7'}, {Keys.NumPad8, '8'}, {Keys.NumPad9, '9'}
    };

    //tells the program which keys are currently being pressed
    private static readonly Dictionary<Keys, bool> IsKeyDown = new()
    {
        {Keys.D0, false}, {Keys.D1, false}, {Keys.D2, false}, {Keys.D3, false}, {Keys.D4, false}, {Keys.D5, false}, {Keys.D6, false}, {Keys.D7, false}, {Keys.D8, false}, {Keys.D9, false},
        {Keys.NumPad0, false}, {Keys.NumPad1, false}, {Keys.NumPad2, false}, {Keys.NumPad3, false}, {Keys.NumPad4, false}, {Keys.NumPad5, false}, {Keys.NumPad6, false}, {Keys.NumPad7, false}, {Keys.NumPad8, false}, {Keys.NumPad9, false}
    };
    
    private static bool _isBackspaceDown;

    private static float _timer; //keeps track of how much time has passed
    private const int NextFrameTime = 500; //the number of milliseconds between frames in the loading animation

    internal static void LoadContent()
    {
        _title = Main.ContentManager.Load<Texture2D>("images/title/title"); //load title texture
        _seedBox = Main.ContentManager.Load<Texture2D>("images/title/seed"); //load seed input box texture
        _startButtonFrames = new []
        {
            Main.ContentManager.Load<Texture2D>("images/title/start/start1"),
            Main.ContentManager.Load<Texture2D>("images/title/start/start2"),
            Main.ContentManager.Load<Texture2D>("images/title/start/start3")
        }; //load start button animation
        _loadingFrames = new[]
        {
            Main.ContentManager.Load<Texture2D>("images/title/loading/loading1"),
            Main.ContentManager.Load<Texture2D>("images/title/loading/loading2"),
            Main.ContentManager.Load<Texture2D>("images/title/loading/loading3"),
            Main.ContentManager.Load<Texture2D>("images/title/loading/loading4")
        }; //load loading animation

        CalculateDrawPositions();
    }

    private static void CalculateDrawPositions()
    {
        //calculates the positions on the screen where each texture will be drawn
        _titlePosition = new Vector2(TitleTarget.Width / 2 - _title.Width / 2, TitleOffset);
        _seedBoxPosition = new Vector2(TitleTarget.Width / 2 - (_seedBox.Width + SeedStartSpace + _startButtonFrames[_startButtonIndex].Width) / 2, TitleOffset + _title.Height + SeedOffset);
        _seedPosition = _seedBoxPosition + new Vector2(25, 2);
        _startRectangle = new Rectangle((int)(_seedBoxPosition.X + _seedBox.Width + SeedStartSpace), (int)_seedBoxPosition.Y + _seedBox.Height / 2 - _startButtonFrames[_startButtonIndex].Height / 2, _startButtonFrames[_startButtonIndex].Width, _startButtonFrames[_startButtonIndex].Height);
        _loadingPosition = new Vector2(TitleTarget.Width - _loadingFrames[_loadingIndex].Width - 1, TitleTarget.Height - _loadingFrames[_loadingIndex].Height - 1);
    }
    
    internal static void Update()
    {
        //keyboard input
        KeyboardState keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Back) && _seed.Length > 0 && !_isBackspaceDown) //if backspace has been pressed while the seed box is not empty
        {
            _seed = _seed[..^1]; //removes the number at the end of the seed input
            _isBackspaceDown = true; //prevents the action from being triggered again until the key has been released
        }
        else if (keyboardState.IsKeyUp(Keys.Back) && _isBackspaceDown) _isBackspaceDown = false; //tells the program the key has been released
        
        //number input
        foreach (Keys key in NumberKeys.Keys)
        {
            if (keyboardState.IsKeyDown(key) && _seed.Length < MaxSeedLength && !IsKeyDown[key]) //if a number key was pressed and the seed box is not full
            {
                _seed += NumberKeys[key]; //adds the number corresponding to the keypress to the end of the seed
                IsKeyDown[key] = true; //prevents the action for this specific key from being triggered until it is released
            }
            else if (keyboardState.IsKeyUp(key) && IsKeyDown[key]) IsKeyDown[key] = false; //tells the program the key has been released
        }

        //mouse input
        MouseState mouseState = Mouse.GetState();
        Point mousePosition = WindowManager.GetMouseInRectangle(TitleTarget.Bounds);

        switch (_startRectangle.Contains(mousePosition), mouseState.LeftButton)
        {
            case (true, ButtonState.Released) when _isStartButtonDown: //if the left mouse button has been released from being pressed while the cursor is hovering over the button
                StateMachine.ChangeState(StateMachine.Action.LoadMap); //Change the state machine's current state to start loading the map
                break;
            case (true, ButtonState.Released): //if the mouse is hovering over the start button but the left mouse button has not been pressed yet
                _startButtonIndex = 1; //start button changes colour to indicate it can be pressed by the player
                break;
            case (true, ButtonState.Pressed):
                _startButtonIndex = 2; //indicates to the player that the button has been pressed
                _isStartButtonDown = true; //tells the program the start button has been pressed
                break;
            default: //the mouse is not hovering over the button
                _startButtonIndex = 0; //start button is up
                _isStartButtonDown = false; //tells the program the start button has not been pressed
                break;
        }
        
        if (keyboardState.IsKeyDown(Keys.Enter)) //if the enter key was pressed
        {
            _startButtonIndex = 2; //indicate to the player that the start button has been pressed
            StateMachine.ChangeState(StateMachine.Action.LoadMap); //Change the state machine's current state to start loading the map
        }
    }

    internal static void LoadMap()
    {
        int seed = _seed == "" ? new Random().Next(0, (int)Math.Pow(10, MaxSeedLength)) : Convert.ToInt32(_seed); //generates a random seed if the user left the input box blank
        Task.Run(() => MapGenerator.GenerateNoiseMap(seed)); //generates the map asynchronously so the user can still move their mouse, resize the window, etc while the map loads
    }
    
    internal static void PlayLoadingAnimation(GameTime gameTime)
    {
        //only executes when the map is being loaded
        if (StateMachine.CurrentState == StateMachine.State.LoadingMap) Ui.PlayAnimation(gameTime, ref _timer, NextFrameTime, ref _loadingIndex, _loadingFrames.Length);
    }

    private static void DrawTitleScreen()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(TitleTarget); //switch to title screen render target
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent); //clear render target to be transparent
        
        Main.SpriteBatch.Begin();
        Main.SpriteBatch.Draw(_title, _titlePosition, TitleColour); //draw title
        Main.SpriteBatch.Draw(_startButtonFrames[_startButtonIndex], _startRectangle, Color.White); //draw start button
        Main.SpriteBatch.Draw(_seedBox, _seedBoxPosition, Color.White); //draw seed input box
        Ui.DrawNumber(_seed, _seedPosition, TextColour); //draw seed in input box if necessary 
        if (StateMachine.CurrentState == StateMachine.State.LoadingMap) Main.SpriteBatch.Draw(_loadingFrames[_loadingIndex], _loadingPosition, TextColour); //draw loading animation if the map is being generated
        Main.SpriteBatch.End();
    }

    internal static void Draw()
    {
        DrawTitleScreen(); //draw everything to the title screen render target
        
        Main.Graphics.GraphicsDevice.SetRenderTarget(WindowManager.Scene); //switch to the scene render target
        Main.Graphics.GraphicsDevice.Clear(BackgroundColour); //clear scene to the title screen's background colour
        
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp); //begin drawing without antialiasing so pixel art does not become blurred when scaled to higher resolutions
        Main.SpriteBatch.Draw(TitleTarget, WindowManager.Scene.Bounds, Color.White); //draw the title screen and scale it to the size of the scene
        Main.SpriteBatch.End();
    }
}