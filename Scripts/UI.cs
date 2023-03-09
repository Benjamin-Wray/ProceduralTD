using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Ui
{
    internal const int UiWidth = 480;
    internal const int UiHeight = 270;
    internal const int HudWidth = UiWidth / 2 ;
    internal const int HudHeight = UiHeight / 2;
    internal static readonly RenderTarget2D UiTarget = new(Main.Graphics.GraphicsDevice, UiWidth, UiHeight); //this render target will contain the HUD
    private static readonly RenderTarget2D HudTarget = new(Main.Graphics.GraphicsDevice, HudWidth, HudHeight); //this render target will contain the HUD

    //colours used when drawing to the screen
    private static readonly Color MenuBackground = new(122, 119, 153);
    private static readonly Color TileBackground = new(58, 69, 104);
    private static readonly Color HoverColour = new(97, 95, 132);
    private static readonly Color SelectedColour = new(134, 144, 178);
    private static readonly Color TextColour = Color.Black;
    internal static readonly Color CanBuyColour = new(255, 240, 43);
    internal static readonly Color CannotBuyColour = new(130, 33, 29);

    //background drawn behind the button icon
    private static Texture2D? _buttonBackground;

    //utility textures
    private static Texture2D? _upgrade;
    private static Texture2D? _sell;
    
    //the order the buttons will be drawn in
    internal static readonly Texture2D?[] ButtonDrawOrder = new Texture2D?[6];

    //counter icon textures
    private static Texture2D _heart;
    private static Texture2D _coin;
    private static Texture2D _wave;

    private static bool _leftMouseDown;
    
    private static int? _hover;

    private static int? _selectedOption;
    internal static int? SelectedOption
    {
        get => _selectedOption;
        set
        {
            _selectedOption = value;
            TowerManager.SelectTower();
        }
    }

    //textures for numbers 0-9
    private static Dictionary<char, Texture2D> _digits;

    //draw positions
    private const int ButtonSize = 32;
    private static readonly Vector2 ButtonPosition = new(168 + ButtonSize / 2, 32 + ButtonSize / 2);
    private static readonly Vector2 HeartPosition = new(168, 3);
    private static Vector2 _healthPosition;
    private static readonly Vector2 CoinPosition = new(168, 17);
    private static Vector2 _moneyPosition;
    private static readonly Vector2 WavePosition = new(4, 4);
    private static Vector2 _waveCountPosition;

    internal static int? CursorPrice;
    private static readonly int?[] Prices = 
    {
        new LandMine().BuyPrice, new Cannon().BuyPrice,
        new NailGun().BuyPrice, new Sniper().BuyPrice,
        null, null
    };

    internal static void LoadContent()
    {
        _buttonBackground = Main.ContentManager.Load<Texture2D>("images/ui/button_background");

        _upgrade = Main.ContentManager.Load<Texture2D>("images/ui/utilities/upgrade"); 
        _sell = Main.ContentManager.Load<Texture2D>("images/ui/utilities/sell");
        
        DrawTowerIcons();

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

    private static void DrawTowerIcons()
    {
        //create array of textures for each tower
        Texture2D?[][] towers =
        {
            new[] {TowerManager.LandMineBase},
            new[] {TowerManager.CannonBase, TowerManager.CannonTop},
            new[] {TowerManager.NailGunBase},
            new[] {TowerManager.SniperBase, TowerManager.SniperTop}
        };

        for (int i = 0; i < towers.Length; i++)
        {
            //create render target to draw tower to
            RenderTarget2D? buttonTexture = new RenderTarget2D(Main.Graphics.GraphicsDevice, ButtonSize, ButtonSize);
            Main.Graphics.GraphicsDevice.SetRenderTarget(buttonTexture);
            Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
            
            //draw tower to render target
            Main.SpriteBatch.Begin();
            foreach (Texture2D? texture in towers[i]) Main.SpriteBatch.Draw(texture, CentrePosition(buttonTexture.Bounds.Center.ToVector2(), texture), Color.White);
            Main.SpriteBatch.End();

            ButtonDrawOrder[i] = buttonTexture;
        }

        //set upgrade and sell button textures
        ButtonDrawOrder[4] = _upgrade;
        ButtonDrawOrder[5] = _sell;
    }
    
    private static void CalculateTextPosition()
    {
        //calculate position to draw the counters
        _healthPosition = HeartPosition + new Vector2(_heart.Width + 2, 2);
        _moneyPosition = CoinPosition + new Vector2(_coin.Width + 2, 2);
        _waveCountPosition = WavePosition + new Vector2(_wave.Width, 0);
    }

    internal static void Update()
    {
        if (Camera.CameraTarget.Bounds.Contains(WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds))) //if the mouse is on the map
        {
            if (SelectedOption < 4 || !_selectedOption.HasValue) CursorPrice = null; //don't draw price by cursor unless hovering over button
            return; //stop mouse input while mouse is on the map
        }
        
        //set hover and cursor price to null at the start of each frame so the hover frame is removed when the mouse is not over a button
        _hover = null; 
        CursorPrice = null;
        
        for (int i = 0; i < ButtonDrawOrder.Length; i++)
        {
            int x = i % 2;
            int y = (int)(i / 2f);
            Vector2 position = new Vector2(x, y) * ButtonSize + ButtonPosition; //position of the button we are checking
            if (x >= 2) position.Y += 8; //the last row will be slightly lower to separate the tower buttons from the utility options
            
            Rectangle buttonBounds = new Rectangle((position - _buttonBackground.Bounds.Size.ToVector2() / 2).ToPoint(), _buttonBackground.Bounds.Size); //the rectangle our button in in
            if (buttonBounds.Contains(WindowManager.GetMouseInRectangle(HudTarget.Bounds))) //check if the mouse is within the bounds of the button
            {
                switch (Mouse.GetState().LeftButton, _leftMouseDown) //check if the left mouse button is down
                {
                    case (ButtonState.Pressed, false): //only executes for the first frame the button is pressed
                        SelectedOption = SelectedOption == i ? null : i; //selects the clicked button but deselects it if the button clicked is already selected
                        _leftMouseDown = true; //mouse is now down and will not select anything until released again
                        break;
                    case (ButtonState.Released, _): //executes when the mouse button is released after being pressed
                        _leftMouseDown = false; //mouse is now released and will select when next pressed
                        break;
                }

                _hover = i; //sets the hover frame to be drawn over the button the mouse is currently over
                CursorPrice = Prices[i]; //sets the price of the tower to be drawn by the cursor
            }
        }
    }
    
    internal static void PlayAnimation(GameTime gameTime, ref float timer, float nextFrameTime, ref int loadingIndex, int frameCount)
    {
        timer += (float)gameTime.ElapsedGameTime.TotalSeconds; //updates the timer since the last frame

        if (timer >= nextFrameTime) //executes once a certain amount of time has passed
        {
            //switches to the next frame in the loading texture's animation
            //MODed by number of frames so the animation loops back to the start once it reaches the end
            loadingIndex = (loadingIndex + 1) % frameCount;
            
            timer %= nextFrameTime; //resets the timer so the loading animation isn't updated until the next time interval has passed
        }
    }

    internal static Vector2 CentrePosition(Vector2 position, Texture2D? texture, float scale = 1) => position - texture.Bounds.Size.ToVector2() * scale / 2f; //offset position of texture so it is drawn at the centre
    
    private static void DrawUi()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(HudTarget);
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
            if (SelectedOption == i) backgroundColour = SelectedColour;
            else if (_hover == i) backgroundColour = HoverColour;
            
            Main.SpriteBatch.Draw(_buttonBackground, CentrePosition(position, _buttonBackground), backgroundColour); //draw the background for the button
            Main.SpriteBatch.Draw(ButtonDrawOrder[i], CentrePosition(position, ButtonDrawOrder[i]), Color.White); //draw the button icon
        }
        
        //draw the icons for each counter
        Main.SpriteBatch.Draw(_heart, HeartPosition, Color.White);
        Main.SpriteBatch.Draw(_coin, CoinPosition, Color.White);
        Main.SpriteBatch.Draw(_wave, WavePosition, TextColour);

        //draw the numbers for health, money and current wave
        DrawNumber(Player.Health.ToString(), _healthPosition, TextColour);
        DrawNumber(Player.Money.ToString(), _moneyPosition, TextColour, true);
        DrawNumber(WaveManager.CurrentWave.ToString(), _waveCountPosition, TextColour);
        
        Main.SpriteBatch.End();
    }

    internal static void DrawCursorPrice(Vector2 mousePosition, Texture2D? cursor, float scale)
    {
        if (CursorPrice == null) return;

        Vector2 drawPosition = mousePosition + new Vector2(cursor.Width * scale, -_digits['£'].Height * scale);
        Color drawColour = Player.Money >= CursorPrice.Value ? CanBuyColour : CannotBuyColour;
        if (_selectedOption == (int)TowerManager.MenuOptions.Sell) drawColour = CanBuyColour;
        DrawNumber(CursorPrice.Value.ToString(), drawPosition, drawColour, true, scale); //draws price of tower next to cursor
    }
    
    internal static void DrawNumber(string number, Vector2 drawPosition, Color colour, bool isPrice = false, float scale = 1)
    {
        if (isPrice) number = '£' + number; //add pound symbol to the front of the number if we are drawing a price
        
        //c# cannot iterate through digits in an integer so we must convert it to a string so it can iterate through each character
        //we then convert the character back into an int and use as an index it to get the corresponding texture from our array of digit textures
        foreach (char digit in number)
        {
            Texture2D texture = _digits[digit];
            Main.SpriteBatch.Draw(texture, drawPosition, null, colour, 0, Vector2.Zero, scale, SpriteEffects.None, 0); //draw the selected texture to the render target
            drawPosition.X += texture.Width * scale + scale; //move the position to the right so the next digit is drawn to the right of the previous one
        }
    }
    
    internal static void Draw()
    {
        DrawUi();
        Camera.DrawMap();

        Main.Graphics.GraphicsDevice.SetRenderTarget(UiTarget); //start drawing to the main render target
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent); //clear scene with the menu background colour

        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Main.SpriteBatch.Draw(HudTarget, UiTarget.Bounds, StateMachine.CurrentState == StateMachine.State.PlaceCastle ? Color.Gray : Color.White); //draw the ui and scale it to the size of the scene
        Main.SpriteBatch.End();
        
        Main.Graphics.GraphicsDevice.SetRenderTarget(WindowManager.Scene); //start drawing to the main render target
        Main.Graphics.GraphicsDevice.Clear(MenuBackground); //clear scene with the menu background colour

        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Main.SpriteBatch.Draw(Camera.CameraTarget, Vector2.Zero, Color.White);
        Main.SpriteBatch.Draw(UiTarget, WindowManager.Scene.Bounds, Color.White);
        Main.SpriteBatch.End();
    }
}