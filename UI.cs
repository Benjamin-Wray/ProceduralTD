﻿using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Ui
{
    internal const int UiWidth = 256;
    internal const int UiHeight = 144;
    private static RenderTarget2D _fixedUiTarget; //this render target will contain all ui elements that do not change while the program is running
    private static RenderTarget2D _uiTarget; //this render target will contain all ui elements that do change while the program is running
    private static RenderTarget2D _mouseTarget; //this render target will contain the mouse

    //colours used when drawing to the screen
    private static readonly Color MenuBackground = new(122, 119, 153);
    private static readonly Color TileBackground = new(58, 69, 104);
    private static readonly Color TextColour = Color.Black;
    private static readonly Color CanBuyColour = new(81, 136, 34);
    private static readonly Color CannotBuyColour = new(130, 33, 29);
    private static readonly Color HoverFrameColour = new(255, 255, 255);
    private static readonly Color SelectedFrameColour = new(255, 246, 79);

    //mouse cursor texture
    private static Texture2D _cursor;

    //texture for the frame drawn around buttons when selected or when hovered over by the mouse
    private static Texture2D _buttonFrame;

    //utility icon textures
    private static Texture2D[] _upgrade;
    private static Texture2D[] _sell;
    
    //the order the buttons will be drawn in
    private static Texture2D[,][] _buttonDrawOrder;

    private static Vector2? _hover;
    private static Vector2? _selected;
    
    //textures for numbers 0-9
    private static Texture2D[] _digits;

    //draw positions
    private static readonly Vector2 ButtonPosition = new(184, 32);
    private static readonly Vector2 HeartPosition = new(186, 3);
    private static readonly Vector2 HealthPosition = new(201, 6);
    private static readonly Vector2 CoinPosition = new(186, 17);
    private static readonly Vector2 MoneyPosition = new(201, 20);
    private static readonly Vector2 WavePosition = new(4, 4);
    private static readonly Vector2 WaveCountPosition = new(30, 4);

    private static int? _cursorPrice;
    private static readonly int?[,] Prices = 
    {
        {LandMine.Price, Cannon.Price},
        {NailGun.Price, Sniper.Price},
        {null, null}
    };

    private const int Money = 45;
    private const int Health = 100;
    private const int CurrentWave = 2;


    internal static void Initialize(GraphicsDevice graphicsDevice)
    {
        //create render targets
        _fixedUiTarget = new RenderTarget2D(graphicsDevice, UiWidth, UiHeight);
        _uiTarget = new RenderTarget2D(graphicsDevice, UiWidth, UiHeight);
        _mouseTarget = new RenderTarget2D(graphicsDevice, WindowManager.SceneWidth, WindowManager.SceneHeight);
    }

    internal static void LoadContent()
    {
        _cursor = Main.ContentMan.Load<Texture2D>("images/cursor");
        
        _buttonFrame = Main.ContentMan.Load<Texture2D>("images/ui/button_frame");

        _upgrade = new[] { Main.ContentMan.Load<Texture2D>("images/ui/utilities/upgrade") }; 
        _sell = new[] { Main.ContentMan.Load<Texture2D>("images/ui/utilities/sell") };
        
        _buttonDrawOrder = new[,]
        {
            {LandMine.TowerTexture, Cannon.TowerTexture},
            {NailGun.TowerTexture, Sniper.TowerTexture},
            {_upgrade, _sell}
        };
        
        _digits = new[]
        {
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/0"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/1"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/2"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/3"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/4"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/5"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/6"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/7"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/8"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/9"),
            Main.ContentMan.Load<Texture2D>("images/ui/text/digits/£")
        };
        
        DrawFixedUi(); //draw the fixed ui to its render target
    }

    private static void DrawFixedUi()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(_fixedUiTarget); //start drawing on UI render target
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent); //clear the render target to transparent so it does not cover the map when drawn

        Main.SpriteBatch.Begin();
        
        //background drawn behind the button icon
        Texture2D buttonBackground = Main.ContentMan.Load<Texture2D>("images/ui/button_background");
        
        //draw the buttons
        for (int x = 0; x < _buttonDrawOrder.GetLength(0); x++)
        {
            for (int y = 0; y < _buttonDrawOrder.GetLength(1); y++)
            {
                Vector2 position = new Vector2(y * 32 + ButtonPosition.X, x * 32 + ButtonPosition.Y); //position of the button to be drawn
                if (x >= 2) position.Y += 8; //the last row will be slightly lower to separate the tower buttons from the utility options
                Main.SpriteBatch.Draw(buttonBackground, position, TileBackground); //draw the background for the button
                foreach (Texture2D texture in _buttonDrawOrder[x, y]) Main.SpriteBatch.Draw(texture, position, Color.White); //draw the button icon
            }
        }
        
        //the icons for indicating the player's current health and balance
        Texture2D heart = Main.ContentMan.Load<Texture2D>("images/ui/icons/heart");
        Main.SpriteBatch.Draw(heart, HeartPosition, Color.White); 
        Texture2D coin = Main.ContentMan.Load<Texture2D>("images/ui/icons/coin");
        Main.SpriteBatch.Draw(coin, CoinPosition, Color.White);
        
        //the text for indicating the current wave
        Texture2D wave = Main.ContentMan.Load<Texture2D>("images/ui/text/wave");
        Main.SpriteBatch.Draw(wave, WavePosition, TextColour);
    
        Main.SpriteBatch.End();
    }

    private static bool _leftMouseDown;
    
    internal static void Update()
    {
        Point mousePosition = WindowManager.GetMouseInRectangle(_uiTarget.Bounds); //gets the position of the mouse on the ui render target
        
        //set hover and cursor price to null at the start of each frame so the hover frame is removed when the mouse is not over a button
        _hover = null; 
        _cursorPrice = null;
        
        for (int x = 0; x < _buttonDrawOrder.GetLength(0); x++)
        {
            for (int y = 0; y < _buttonDrawOrder.GetLength(1); y++)
            {
                Vector2 buttonPosition = new Vector2(y * 32 + ButtonPosition.X, x * 32 + ButtonPosition.Y); //position of the button we are checking
                if (x >= 2) buttonPosition.Y += 8; //the last row will be slightly lower to separate the tower buttons from the utility options
                if (mousePosition.X >= buttonPosition.X + 1 && mousePosition.X < buttonPosition.X + 31 && mousePosition.Y >= buttonPosition.Y + 1 && mousePosition.Y < buttonPosition.Y + 31) //check if the mouse is within the bounds of the button
                {
                    switch (Main.MouseState.LeftButton) //check if the left mouse button is down
                    {
                        case ButtonState.Pressed when !_leftMouseDown: //only executes for the first frame the button is pressed
                            _selected = _selected != buttonPosition ? buttonPosition : null; //selects the clicked button but deselects it if the button clicked is already selected
                            _leftMouseDown = true; //mouse is now down and will not select anything until released again
                            break;
                        case ButtonState.Released when _leftMouseDown: //executes when the mouse button is released after being pressed
                            _leftMouseDown = false; //mouse is now released and will select when next pressed
                            break;
                    }

                    _hover = buttonPosition; //sets the hover frame to be drawn over the button the mouse is currently over
                    _cursorPrice = Prices[x, y]; //sets the price of the tower to be drawn by the cursor
                }
            }
        }
    }

    private static void DrawMouse()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(_mouseTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent); //cleared to transparent so it can be drawn on top of everything else

        Vector2 mousePosition = WindowManager.GetMouseInRectangle(_mouseTarget.Bounds).ToVector2(); //get the position of the mouse on the render target
        
        Main.SpriteBatch.Begin();
        Main.SpriteBatch.Draw(_cursor, mousePosition, Color.White); //draw the mouse to the render target
        if (_cursorPrice != null) DrawNumber(_cursorPrice.Value, mousePosition + new Vector2(_cursor.Width / 2f, -_digits[10].Height), Money >= _cursorPrice.Value ? CanBuyColour : CannotBuyColour, true); //draws price of tower next to cursor
        Main.SpriteBatch.End();
    }

    private static void DrawUi()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(_uiTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent); //cleared to transparent so it can be drawn on top of everything else
        
        Main.SpriteBatch.Begin();
        
        Main.SpriteBatch.Draw(_fixedUiTarget, Vector2.Zero, Color.White); //draw the fixed ui so the dynamic ui can be drawn on top of it

        //draw the hover and selected button frames if needed
        if (_hover != null) Main.SpriteBatch.Draw(_buttonFrame, _hover.Value, HoverFrameColour);
        if (_selected != null) Main.SpriteBatch.Draw(_buttonFrame, _selected.Value, SelectedFrameColour);

        //draw the numbers for health, money and current wave
        DrawNumber(Health, HealthPosition, TextColour);
        DrawNumber(Money, MoneyPosition, TextColour, true);
        DrawNumber(CurrentWave, WaveCountPosition, TextColour);
        
        Main.SpriteBatch.End();
    }

    private static void DrawNumber(int number, Vector2 drawPosition, Color colour, bool isPrice = false)
    {
        if (isPrice)
        {
            Main.SpriteBatch.Draw(_digits[10], drawPosition, colour); //draw the selected texture to the render target
            drawPosition.X += _digits[10].Width + 1; //move the position to the right so the next digit is drawn to the right of the previous one
        }
        
        //c# cannot iterate through digits in an integer so we must convert it to a string so it can iterate through each character
        //we then convert the character back into an int and use as an index it to get the corresponding texture from our array of digit textures
        foreach (var texture in number.ToString().Select(digit => _digits[Convert.ToInt32(Convert.ToString(digit))]))
        {
            Main.SpriteBatch.Draw(texture, drawPosition, colour); //draw the selected texture to the render target
            drawPosition.X += texture.Width + 1; //move the position to the right so the next digit is drawn to the right of the previous one
        }
    }
    
    internal static void Draw()
    {
        DrawMouse();
        DrawUi();

        Main.Graphics.GraphicsDevice.SetRenderTarget(WindowManager.Scene); //start drawing to the main render target
        Main.Graphics.GraphicsDevice.Clear(MenuBackground); //clear scene with the menu background colour

        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Camera.DrawMap(); //draw the map the game takes place on

        //draw the ui and scale it to the size of the scene
        Main.SpriteBatch.Draw(_uiTarget, new Rectangle(0, 0, WindowManager.Scene.Width, WindowManager.Scene.Height), Color.White);
        Main.SpriteBatch.Draw(_mouseTarget, new Rectangle(0, 0, WindowManager.Scene.Width, WindowManager.Scene.Height), Color.White);
        Main.SpriteBatch.Draw(_mouseTarget, new Rectangle(0, 0, WindowManager.Scene.Width, WindowManager.Scene.Height), Color.White);

        Main.SpriteBatch.End();
    }
}