using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class StateMachine
{
    private static KeyboardState _keyState;
    private static MouseState _mouseState;
    
    private enum State
    {
        Title,
        LoadingMap,
        MainGame
    }

    internal enum Action
    {
        Initialize,
        LoadMap,
        BeginGame
    }

    private static State _currentState;

    internal static void ChangeState(Action action)
    {
        switch (_currentState, action)
        {
            case (_, Action.Initialize):
                _currentState = State.Title;
                break;
            case (State.Title, Action.LoadMap):
                _currentState = State.LoadingMap;
                Task.Run(() => MapGenerator.GenerateNoiseMap());
                break;
            case (State.LoadingMap, Action.BeginGame):
                _currentState = State.MainGame;
                Camera.GenerateColourMap();
                break;
        }
    }

    internal static void Initialize()
    {
        WindowManager.Initialize();
        ChangeState(Action.Initialize);
    }

    internal static void LoadContent()
    {
        WindowManager.LoadContent();
        TitleScreen.LoadContent();
        Ui.LoadContent();
        Camera.LoadContent();
    }

    internal static void Update()
    {
        _keyState = Keyboard.GetState();
        _mouseState = Mouse.GetState();
        
        WindowManager.Update(_keyState);

        switch (_currentState)
        {
            case State.Title:
                TitleScreen.Update(_keyState);
                break;
            case State.MainGame:
                Camera.Update(_keyState);
                Ui.Update(_mouseState);
                break;
        }
    }

    internal static void Draw()
    {
        switch (_currentState)
        {
            case State.Title:
                TitleScreen.Draw();
                break;
            case State.MainGame:
                Ui.Draw();
                break;
        }
        WindowManager.Draw(_mouseState.Position);
    }
}