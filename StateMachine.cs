using Microsoft.Xna.Framework;

namespace ProceduralTD;

internal static class StateMachine
{
    internal enum State
    {
        Title,
        LoadingMap,
        PlaceBase,
        Wave,
        Downtime
    }

    internal enum Action
    {
        StartProgram,
        LoadMap,
        BeginGame
    }

    //the state machine's current state
    //other classes can read it but not directly assign it
    //other classes can only change the state machine's state by passing an action into the ChangeState subroutine
    internal static State CurrentState { get; private set; }

    //used to change the current state
    //an action is input to the state machine and the state will change depending on the action and current state
    internal static void ChangeState(Action action)
    {
        switch (CurrentState, action)
        {
            case (_, Action.StartProgram): //runs when the program first starts
                CurrentState = State.Title; //switches to the title screen
                WindowManager.Initialize(); //initializes the window manager before starting the game
                break;
            case (State.Title, Action.LoadMap): //runs when the start button is pressed on the title screen
                CurrentState = State.LoadingMap; //tells the program to start loading the map
                TitleScreen.LoadMap();
                break;
            case (State.LoadingMap, Action.BeginGame): //runs when the map has finished being generated
                CurrentState = State.PlaceBase;
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
        Camera.LoadContent();
        Ui.LoadContent();
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
            case State.PlaceBase:
            case State.Wave:
                Camera.MoveCamera(gameTime);
                TowerPlacement.Update();
                Ui.Update();
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
            case State.PlaceBase:
                Ui.Draw();
                break;
        }
        WindowManager.Draw(); //the window manager draws the scene to the window after the other classes have finished drawing to it 
    }
}