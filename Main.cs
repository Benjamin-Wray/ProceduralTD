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

        private static bool _exit;

        public Main()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            IsFixedTimeStep = true;
            TargetElapsedTime = System.TimeSpan.FromSeconds(1d / 165);

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
            if (_exit) Exit();

            StateMachine.Update(gameTime);

            base.Update(gameTime);
        }
        
        public static void ExitGame()
        {
            _exit = true;
        }
        
        protected override void Draw(GameTime gameTime)
        {
            StateMachine.Draw();
            
            base.Draw(gameTime);
        }
    }
}