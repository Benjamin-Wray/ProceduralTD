using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Ui
{
    internal enum Towers
    {
        LandMine,
        Cannon,
        NailGun,
        Sniper,
        Upgrade,
        Sell
    }

    internal const int UiWidth = 256;
    internal const int UiHeight = 144;
    private static readonly RenderTarget2D UiTarget = new(Main.Graphics.GraphicsDevice, UiWidth, UiHeight); //this render target will contain all ui elements that do change while the program is running

    //colours used when drawing to the screen
    private static readonly Color MenuBackground = new(122, 119, 153);
    private static readonly Color TileBackground = new(58, 69, 104);
    private static readonly Color HoverColour = new(97, 95, 132);
    private static readonly Color SelectedColour = new(134, 144, 178);
    private static readonly Color TextColour = Color.Black;
    internal static readonly Color CanBuyColour = new(81, 136, 34);
    internal static readonly Color CannotBuyColour = new(130, 33, 29);

    //background drawn behind the button icon
    private static Texture2D _buttonBackground;

    //tower textures
    internal static Texture2D[] LandMineTexture;
    internal static Texture2D[] CannonTexture;
    internal static Texture2D[] NailGunTexture;
    internal static Texture2D[] SniperTexture;
    
    //utility textures
    private static Texture2D[] _upgrade;
    private static Texture2D[] _sell;
    
    //the order the buttons will be drawn in
    internal static Texture2D[][] ButtonDrawOrder;

    //counter icon textures
    private static Texture2D _heart;
    private static Texture2D _coin;
    private static Texture2D _wave;

    private static bool _leftMouseDown;
    
    private static int? _hover;
    internal static int? Selected;
    
    //textures for numbers 0-9
    private static Dictionary<char, Texture2D> _digits;

    //draw positions
    private const int ButtonSize = 32;
    private static readonly Vector2 ButtonPosition = new(184 + ButtonSize / 2, 32 + ButtonSize / 2);
    private static readonly Vector2 HeartPosition = new(186, 3);
    private static Vector2 _healthPosition;
    private static readonly Vector2 CoinPosition = new(186, 17);
    private static Vector2 _moneyPosition;
    private static readonly Vector2 WavePosition = new(4, 4);
    private static Vector2 _waveCountPosition;

    private static int? _cursorPrice;
    private static readonly int?[] Prices = 
    {
        LandMine.Price, Cannon.Price,
        NailGun.Price, Sniper.Price,
        null, null
    };

    private const int Money = 45;
    private const int Health = 100;
    private const int CurrentWave = 2;

    internal static void LoadContent()
    {
        _buttonBackground = Main.ContentManager.Load<Texture2D>("images/ui/button_background");

        LandMineTexture = new[] { Main.ContentManager.Load<Texture2D>("images/ui/towers/landmine") };
        CannonTexture = new []
        {
            Main.ContentManager.Load<Texture2D>("images/ui/towers/cannon_base"),
            Main.ContentManager.Load<Texture2D>("images/ui/towers/cannon_top")
        };
        NailGunTexture = new[]
        {
            Main.ContentManager.Load<Texture2D>("images/ui/towers/nailgun_base"),
            Main.ContentManager.Load<Texture2D>("images/ui/towers/nailgun_top")
        };
        SniperTexture = new []
        {
            Main.ContentManager.Load<Texture2D>("images/ui/towers/sniper_base"),
            Main.ContentManager.Load<Texture2D>("images/ui/towers/sniper_top")
        };
        
        _upgrade = new[] { Main.ContentManager.Load<Texture2D>("images/ui/utilities/upgrade") }; 
        _sell = new[] { Main.ContentManager.Load<Texture2D>("images/ui/utilities/sell") };

        ButtonDrawOrder = new[]
        {
            LandMineTexture, CannonTexture,
            NailGunTexture, SniperTexture,
            _upgrade, _sell
        };

        //the icons for indicating the player's current health and balance
        _heart = Main.ContentManager.Load<Texture2D>("images/ui/icons/heart");
        _coin = Main.ContentManager.Load<Texture2D>("images/ui/icons/coin");
        _wave = Main.ContentManager.Load<Texture2D>("images/ui/text/wave");
        
        _digits = new Dictionary<char, Texture2D>
        {
            { '0', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/0") },
            { '1', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/1") },
            { '2', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/2") },
            { '3', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/3") },
            { '4', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/4") },
            { '5', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/5") },
            { '6', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/6") },
            { '7', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/7") },
            { '8', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/8") },
            { '9', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/9") },
            { '£', Main.ContentManager.Load<Texture2D>("images/ui/text/digits/£") }
        };
        
        CalculateTextPosition();
    }

    private static void CalculateTextPosition()
    {
        _healthPosition = HeartPosition + new Vector2(_heart.Width + 2, 2);
        _moneyPosition = CoinPosition + new Vector2(_coin.Width + 2, 2);
        _waveCountPosition = WavePosition + new Vector2(_wave.Width, 0);
    }

    internal static void Update()
    {
        MouseState mouseState = Mouse.GetState();
        Point mousePosition = WindowManager.GetMouseInRectangle(UiTarget.Bounds); //gets the position of the mouse on the ui render target
        
        //set hover and cursor price to null at the start of each frame so the hover frame is removed when the mouse is not over a button
        _hover = null; 
        _cursorPrice = null;
        
        for (int i = 0; i < ButtonDrawOrder.Length; i++)
        {
            int x = i % 2;
            int y = (int)Math.Floor(i / 2f);
            Vector2 position = new Vector2(x, y) * ButtonSize + ButtonPosition; //position of the button we are checking
            if (x >= 2) position.Y += 8; //the last row will be slightly lower to separate the tower buttons from the utility options
            
            Rectangle buttonBounds = new Rectangle((position - _buttonBackground.Bounds.Size.ToVector2() / 2).ToPoint(), _buttonBackground.Bounds.Size); //the rectangle our button in in
            if (buttonBounds.Contains(mousePosition)) //check if the mouse is within the bounds of the button
            {
                switch (mouseState.LeftButton, _leftMouseDown) //check if the left mouse button is down
                {
                    case (ButtonState.Pressed, false): //only executes for the first frame the button is pressed
                        Selected = Selected == i ? null : i; //selects the clicked button but deselects it if the button clicked is already selected
                        _leftMouseDown = true; //mouse is now down and will not select anything until released again
                        break;
                    case (ButtonState.Released, _): //executes when the mouse button is released after being pressed
                        _leftMouseDown = false; //mouse is now released and will select when next pressed
                        break;
                }

                _hover = i; //sets the hover frame to be drawn over the button the mouse is currently over
                _cursorPrice = Prices[i]; //sets the price of the tower to be drawn by the cursor
            }
        }
    }

    internal static Vector2 CentrePosition(Vector2 position, Texture2D texture)
    {
        return position - texture.Bounds.Size.ToVector2() / 2f;
    }
    
    private static void DrawUi()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(UiTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent); //cleared to transparent so it can be drawn on top of everything else
        
        Main.SpriteBatch.Begin();
        
        //draw the buttons
        for (int i = 0; i < ButtonDrawOrder.Length; i++)
        {
            int x = i % 2;
            int y = (int)Math.Floor(i / 2f);
            Vector2 position = new Vector2(x, y) * ButtonSize + ButtonPosition; //position of the button to be drawn
            if (x >= 2) position.Y += 8; //the last row will be slightly lower to separate the tower buttons from the utility options
            
            //select colour
            Color backgroundColour = TileBackground;
            if (Selected == i) backgroundColour = SelectedColour;
            else if (_hover == i) backgroundColour = HoverColour;
            
            Main.SpriteBatch.Draw(_buttonBackground, CentrePosition(position, _buttonBackground), backgroundColour); //draw the background for the button
            foreach (Texture2D texture in ButtonDrawOrder[i]) Main.SpriteBatch.Draw(texture, CentrePosition(position, texture), Color.White); //draw the button icon
        }
        
        //draw the icons for each counter
        Main.SpriteBatch.Draw(_heart, HeartPosition, Color.White);
        Main.SpriteBatch.Draw(_coin, CoinPosition, Color.White);
        Main.SpriteBatch.Draw(_wave, WavePosition, TextColour);

        //draw the numbers for health, money and current wave
        DrawNumber(Health.ToString(), _healthPosition, TextColour);
        DrawNumber(Money.ToString(), _moneyPosition, TextColour, true);
        DrawNumber(CurrentWave.ToString(), _waveCountPosition, TextColour);
        
        Main.SpriteBatch.End();
    }

    internal static void DrawPrice(Vector2 mousePosition, Texture2D cursor)
    {
        if (_cursorPrice == null) return;
        
        Vector2 drawPosition = mousePosition + new Vector2(cursor.Width / 2f, -_digits['£'].Height);
        Color drawColour = Money >= _cursorPrice.Value ? CanBuyColour : CannotBuyColour;
        DrawNumber(_cursorPrice.Value.ToString(), drawPosition, drawColour, true); //draws price of tower next to cursor
    }
    
    internal static void DrawNumber(string number, Vector2 drawPosition, Color colour, bool isPrice = false)
    {
        if (isPrice)
        {
            Main.SpriteBatch.Draw(_digits['£'], drawPosition, colour); //draw the selected texture to the render target
            drawPosition.X += _digits['£'].Width + 1; //move the position to the right so the next digit is drawn to the right of the previous one
        }
        
        //c# cannot iterate through digits in an integer so we must convert it to a string so it can iterate through each character
        //we then convert the character back into an int and use as an index it to get the corresponding texture from our array of digit textures
        foreach (char digit in number)
        {
            Texture2D texture = _digits[digit];
            Main.SpriteBatch.Draw(texture, drawPosition, colour); //draw the selected texture to the render target
            drawPosition.X += texture.Width + 1; //move the position to the right so the next digit is drawn to the right of the previous one
        }
    }
    
    internal static void Draw()
    {
        DrawUi();
        Camera.DrawMap();
        TowerPlacement.Draw();

        Main.Graphics.GraphicsDevice.SetRenderTarget(WindowManager.Scene); //start drawing to the main render target
        Main.Graphics.GraphicsDevice.Clear(MenuBackground); //clear scene with the menu background colour

        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        //draw the ui and scale it to the size of the scene
        Main.SpriteBatch.Draw(Camera.CameraTarget, Camera.CameraTarget.Bounds, Color.White);
        Main.SpriteBatch.Draw(TowerPlacement.TowerTarget, Vector2.Zero, Color.White);
        Main.SpriteBatch.Draw(UiTarget, WindowManager.Scene.Bounds, Color.White);

        Main.SpriteBatch.End();
    }
}