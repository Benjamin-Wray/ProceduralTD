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

        internal Main()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false; //system mouse cursor is hidden
            IsFixedTimeStep = true;
        }

        protected override void Initialize() //runs at start of program
        {
            GameWindow = Window;
            StateMachine.Initialize();
            base.Initialize();
        }

        protected override void LoadContent() //used for loading textures into memory
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            ContentManager = Content;
            
            StateMachine.LoadContent();
        }

        protected override void Update(GameTime gameTime) //runs every frame
        {
            StateMachine.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) //runs every frame after update
        {
            StateMachine.Draw();
            base.Draw(gameTime);
        }
    }
}