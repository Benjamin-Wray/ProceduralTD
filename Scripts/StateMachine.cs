using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ProceduralTD;

internal static class StateMachine
{
    internal enum State
    {
        InitialState,
        Title,
        LoadingMap,
        PlaceCastle,
        Wave,
        GameOver,
        StartUp
    }

    internal enum Action
    {
        StartProgram,
        LoadMap,
        BeginGame,
        PlaceCastle,
        EndGame,
        GoToTitle
    }

    //the state machine's current state
    //other classes can read it but not directly assign it
    //other classes can only change the state machine's state by passing an action into the ChangeState subroutine
    internal static State CurrentState { get; private set; } = State.InitialState;

    //used to change the current state
    //an action is input to the state machine and the state will change depending on the action and current state
    internal static void ChangeState(Action action)
    {
        switch (CurrentState, action)
        {
            case (State.InitialState, Action.StartProgram):
                CurrentState = State.StartUp;
                WindowManager.Initialize();
                ChangeState(Action.GoToTitle);
                break;
            case (State.StartUp, Action.GoToTitle):
            case (State.GameOver, Action.GoToTitle):
                CurrentState = State.Title;
                TitleScreen.Initialize();
                break;
            case (State.Title, Action.LoadMap):
                CurrentState = State.LoadingMap;
                Task.Run(MapGenerator.GenerateNoiseMap); //generates the map asynchronously so the user can still move their mouse, resize the window, etc while the map loads
                break;
            case (State.LoadingMap, Action.BeginGame):
                CurrentState = State.PlaceCastle;
                WaveManager.Initialize();
                TowerManager.Initialize();
                break;
            case (State.PlaceCastle, Action.PlaceCastle):
                CurrentState = State.Wave;
                WaveManager.StartWaves();
                break;
            case (State.Wave, Action.EndGame):
                CurrentState = State.GameOver;
                GameOverScreen.Initialize();
                break;
        }
    }

    //runs at the start of the program
    internal static void Initialize()
    {
        ChangeState(Action.StartProgram); //sets the current state to it's initial state: the title screen
    }

    //loads all of the textures to be drawn
    internal static void LoadContent()
    {
        WindowManager.LoadContent();
        TitleScreen.LoadContent();
        TowerManager.LoadContent();
        WaveManager.LoadContent();
        Ui.LoadContent();
        GameOverScreen.LoadContent();
    }

    //runs every frame, mainly used for user input
    internal static void Update(GameTime gameTime)
    {
        WindowManager.Update(); //always runs the window manager regardless of the current state

        switch (CurrentState)
        {
            case State.Title: //the title screen is still displayed while the map is being generated
                TitleScreen.Update();
                break;
            case State.LoadingMap:
                TitleScreen.PlayLoadingAnimation(gameTime);
                break;
            case State.PlaceCastle:
                Camera.Update(gameTime);
                TowerManager.Update(gameTime);
                break;
            case State.Wave:
                Camera.Update(gameTime);
                Ui.Update();
                TowerManager.Update(gameTime);
                WaveManager.Update(gameTime);
                break;
            case State.GameOver:
                GameOverScreen.Update();
                break;
        }
    }

    //runs every frame after update, draws all of the textures to the window
    internal static void Draw()
    {
        //the classes draw to the scene render target
        switch (CurrentState)
        {
            case State.Title:
            case State.LoadingMap: //the title screen is still displayed while the map is being generated
                TitleScreen.Draw();
                break;
            case State.PlaceCastle:
            case State.Wave:
                Ui.Draw();
                break;
            case State.GameOver:
                GameOverScreen.Draw();
                break;
        }
        WindowManager.Draw(); //the window manager draws the scene to the window after the other classes have finished drawing to it 
    }
}