using System;

namespace ProceduralTD;

internal static class Player
{
    private const int MaxMoney = 999999;
    private static int _money = 100;
    internal static int Money
    {
        get => _money;
        set
        {
            _money = Math.Clamp(value, 0, MaxMoney);
            TowerPlacement.SelectedTower?.CheckTowerCanBePlaced();
        }
    }

    private const int MaxHealth = 100;
    private static int _health = MaxHealth;
    internal static int Health
    {
        get => _health;
        set => _health = Math.Clamp(value, 0, MaxHealth);
    }

    internal static Castle Castle;
}