using System;

namespace ProceduralTD;

internal static class Player
{
    private const int MaxMoney = 99999;
    private static int _money = MaxMoney;
    internal static int Money
    {
        get => _money;
        set => _money = Math.Clamp(value, 0, MaxMoney);
    }

    private const int MaxHealth = 100;
    private static int _health = MaxHealth;
    internal static int Health
    {
        get => _health;
        set => _health = Math.Clamp(value, 0, MaxHealth);
    }
    
    internal static int CurrentWave = 1;

    internal static Castle Castle;
}