using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD
{
    internal class Main : Game
    {
        internal static GraphicsDeviceManager Graphics { get; private set; }
        internal static SpriteBatch SpriteBatch { get; private set; }
        internal static ContentManager ContentMan { get; private set; }

        internal static KeyboardState KeyState { get; private set; }
        internal static MouseState MouseState { get; private set; }
        
        //textures
        internal static Texture2D Pixel { get; private set; }

        private float[,] _heightMap;

        public Main()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        protected override void Initialize()
        {
            WindowManager.Initialize(Window);
            Ui.Initialize(GraphicsDevice);

            _heightMap = MapGenerator.GenerateNoiseMap(new Random().Next());

            Camera.Initialize(_heightMap);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            ContentMan = Content;
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            Pixel = Content.Load<Texture2D>("images/map/pixel");
            
            Ui.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyState = Keyboard.GetState();
            MouseState = Mouse.GetState();
            
            if (KeyState.IsKeyDown(Keys.Escape)) Exit();
            
            WindowManager.Update();
            Camera.Update();
            Ui.Update();
            
            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            WindowManager.Draw();
            
            base.Draw(gameTime);
        }
    }
}