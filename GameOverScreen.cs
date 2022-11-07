using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class GameOverScreen
{
    private static readonly RenderTarget2D GameOverTarget = new(Main.Graphics.GraphicsDevice, Ui.HudWidth / 2, Ui.HudHeight / 2);
    
    //textures
    private static Texture2D _gameOver;
    private static Texture2D _playAgain;
    private static Texture2D[] _yesButtonFrames;
    private static int _yesButtonIndex;
    private static Texture2D[] _noButtonFrames;
    private static int _noButtonIndex;

    //colours
    private static readonly Color BackgroundColour = new(24, 31, 47);
    private static readonly Color GameOverColour = new(209, 170, 57);
    private static readonly Color PlayAgainColour = new(169, 209, 193);

    //used for calculating position of textures to be drawn
    private const int GameOverOffset = 4; //distance of title from the top of the window
    private const int PlayAgainOffset = 4; //distance of the seed input box from the title
    private const int YesNoOffset = 12; //distance of the seed input box from the title
    private const int YesNoSpace = 8; //space between the seed box and the start button
    
    //the positions of each texture on the screen
    private static Vector2 _gameOverPosition;
    private static Vector2 _playAgainPosition;
    private static Rectangle _yesRectangle; //the start button uses a rectangle so the program can check if the mouse is hovering over it
    private static Rectangle _noRectangle; //the start button uses a rectangle so the program can check if the mouse is hovering over it

    private static bool _isYesButtonDown; //tells the program if the yes button is currently down
    private static bool _isNoButtonDown; //tells the program if the no button is currently down

    internal static void Initialize()
    {
        _yesButtonIndex = 0;
        _noButtonIndex = 0;
        _isYesButtonDown = false;
        _isNoButtonDown = false;
    }
    
    internal static void LoadContent()
    {
        _gameOver = Main.ContentManager.Load<Texture2D>("images/game over/game over"); //load title texture
        _playAgain = Main.ContentManager.Load<Texture2D>("images/game over/play again"); //load title texture
        _yesButtonFrames = new []
        {
            Main.ContentManager.Load<Texture2D>("images/game over/yes button/yes button1"),
            Main.ContentManager.Load<Texture2D>("images/game over/yes button/yes button2"),
            Main.ContentManager.Load<Texture2D>("images/game over/yes button/yes button3")
        }; //load yes button animation
        _noButtonFrames = new []
        {
            Main.ContentManager.Load<Texture2D>("images/game over/no button/no button1"),
            Main.ContentManager.Load<Texture2D>("images/game over/no button/no button2"),
            Main.ContentManager.Load<Texture2D>("images/game over/no button/no button3")
        }; //load no button animation

        CalculateDrawPositions();
    }

    private static void CalculateDrawPositions()
    {
        //calculates the positions on the screen where each texture will be drawn
        _gameOverPosition = new Vector2(GameOverTarget.Width / 2 - _gameOver.Width / 2, GameOverOffset);
        _playAgainPosition = new Vector2(GameOverTarget.Width / 2 - _playAgain.Width / 2, GameOverOffset + PlayAgainOffset + _gameOver.Height);
        _yesRectangle = new Rectangle(GameOverTarget.Width / 2 - (_yesButtonFrames[_yesButtonIndex].Width + YesNoSpace + _noButtonFrames[_noButtonIndex].Width) / 2, (int)_playAgainPosition.Y + _playAgain.Height + YesNoOffset, _yesButtonFrames[_yesButtonIndex].Width, _yesButtonFrames[_yesButtonIndex].Height);
        _noRectangle = new Rectangle(_yesRectangle.Right + YesNoSpace, _yesRectangle.Center.Y - _noButtonFrames[_noButtonIndex].Height / 2, _noButtonFrames[_noButtonIndex].Width, _noButtonFrames[_noButtonIndex].Height);
    }
    
    internal static void Update()
    {
        if (TitleScreen.UpdateButton(_yesRectangle, ref _isYesButtonDown, ref _yesButtonIndex)) StateMachine.ChangeState(StateMachine.Action.GoToTitle);
        if (TitleScreen.UpdateButton(_noRectangle, ref _isNoButtonDown, ref _noButtonIndex)) Program.Game.Exit();
    }

    private static void DrawGameOverScreen()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(GameOverTarget); //switch to title screen render target
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent); //clear render target to be transparent
        
        Main.SpriteBatch.Begin();
        Main.SpriteBatch.Draw(_gameOver, _gameOverPosition, GameOverColour); //draw title
        Main.SpriteBatch.Draw(_playAgain, _playAgainPosition, PlayAgainColour); //draw title
        Main.SpriteBatch.Draw(_yesButtonFrames[_yesButtonIndex], _yesRectangle, Color.White); //draw start button
        Main.SpriteBatch.Draw(_noButtonFrames[_noButtonIndex], _noRectangle, Color.White); //draw start button
        Main.SpriteBatch.End();
    }

    internal static void Draw()
    {
        DrawGameOverScreen(); //draw everything to the title screen render target
        
        Main.Graphics.GraphicsDevice.SetRenderTarget(WindowManager.Scene); //switch to the scene render target
        Main.Graphics.GraphicsDevice.Clear(BackgroundColour); //clear scene to the title screen's background colour
        
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp); //begin drawing without antialiasing so pixel art does not become blurred when scaled to higher resolutions
        Main.SpriteBatch.Draw(GameOverTarget, WindowManager.Scene.Bounds, Color.White); //draw the title screen and scale it to the size of the scene
        Main.SpriteBatch.End();
    }
}