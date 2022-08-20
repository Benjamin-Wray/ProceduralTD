using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal abstract class Tower
{
    
}

internal class LandMine : Tower
{
    internal static readonly Texture2D[] TowerTexture = { Main.ContentMan.Load<Texture2D>("images/ui/towers/landmine") };
    internal const int Price = 10;
}

internal class Cannon : Tower
{
    internal static readonly Texture2D[] TowerTexture =
    {
        Main.ContentMan.Load<Texture2D>("images/ui/towers/cannon_base"),
        Main.ContentMan.Load<Texture2D>("images/ui/towers/cannon_top")
    };
    internal const int Price = 30;
}

internal class NailGun : Tower
{
    internal static readonly Texture2D[] TowerTexture = {Main.ContentMan.Load<Texture2D>("images/ui/towers/nailgun")};
    internal const int Price = 45;
}

internal class Sniper : Tower
{
    internal static readonly Texture2D[] TowerTexture =
    {
        Main.ContentMan.Load<Texture2D>("images/ui/towers/sniper_base"),
        Main.ContentMan.Load<Texture2D>("images/ui/towers/sniper_top")
    };
    internal const int Price = 75;
}