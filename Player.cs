using System;

namespace ProceduralTD;

internal static class Player
{
    private const int MaxMoney = 999999;
    internal const int StartingMoney = 100;
    private static int _money;
    internal static int Money
    {
        get => _money;
        set
        {
            _money = Math.Clamp(value, 0, MaxMoney);
            TowerManager.SelectedTower?.CheckTowerCanBePlaced();
        }
    }

    internal const int MaxHealth = 100;
    private static int _health;
    internal static int Health
    {
        get => _health;
        set
        {
            _health = Math.Clamp(value, 0, MaxHealth);
            if (_health == 0) StateMachine.ChangeState(StateMachine.Action.EndGame);
        }
    }
}