using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD
{
    internal class Main : Game
    {
        internal static GraphicsDeviceManager Graphics;
        internal static SpriteBatch SpriteBatch;
        internal static ContentManager ContentManager;
        internal static GameWindow GameWindow;

        public Main()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            IsFixedTimeStep = true;
        }

        protected override void Initialize()
        {
            GameWindow = Window;
            
            StateMachine.Initialize();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            ContentManager = Content;
            
            StateMachine.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            StateMachine.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            StateMachine.Draw();
            
            base.Draw(gameTime);
        }
    }
}