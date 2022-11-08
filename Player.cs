using System;

namespace ProceduralTD;

internal static class Player
{
    private const int MaxMoney = 999999; //player's money will not exceed this value
    internal const int StartingMoney = 100; //money the player has at the start of the game
    private static int _money;
    internal static int Money
    {
        get => _money;
        set
        {
            _money = Math.Clamp(value, 0, MaxMoney); //prevents money from being negative or exceeding maximum value
            TowerManager.SelectedTower?.CheckTowerCanBePlaced(); //checks if the selected tower can be afforded with new amount of money
        }
    }

    internal const int StartingHealth = 100; //health the player has at the start of the game
    private static int _health;
    internal static int Health
    {
        get => _health;
        set
        {
            _health = Math.Clamp(value, 0, StartingHealth); //prevents health from being negative
            if (_health == 0) StateMachine.ChangeState(StateMachine.Action.EndGame); //when the player's health reaches zero, the game ends
        }
    }
}