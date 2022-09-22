using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal abstract class Tower
{
    internal Texture2D[] TowerTexture;
    internal Point Position;

    protected Tower(Point position)
    {
         Position = position;
    }

    internal void InvalidateSpace()
    {
        Point newPosition = Ui.CentrePosition(Position.ToVector2(), TowerTexture[0]).ToPoint();
        for (int y = 0; y < TowerTexture[0].Height; y++)
        {
            for (int x = 0; x < TowerTexture[0].Width; x++)
            {
                if (TowerPlacement.IsInRange(newPosition + new Point(x, y), Position, TowerTexture[0].Width / 2f)) continue;
                TowerPlacement.InvalidPositions[newPosition.X + x, newPosition.Y + y] = true;
            }
        }
    }

    protected internal void DrawTower()
    {
        foreach (Texture2D texture in TowerTexture)
        {
            Main.SpriteBatch.Draw(texture, Ui.CentrePosition(Position.ToVector2() - Camera.CameraPosition, texture), Color.White);
        }
    }
}

internal class LandMine : Tower
{
    internal const int Price = 10;

    internal LandMine(Point position) : base(position)
    {
        TowerTexture = Ui.LandMineTexture;
    }
}

internal class Cannon : Tower
{
    internal const int Price = 30;

    internal Cannon(Point position) : base(position)
    {
        TowerTexture = Ui.CannonTexture;
    }
}

internal class NailGun : Tower
{
    internal const int Price = 45;

    internal NailGun(Point position) : base(position)
    {
        TowerTexture = Ui.NailGunTexture;
    }
}

internal class Sniper : Tower
{
    internal const int Price = 75;

    internal Sniper(Point position) : base(position)
    {
        TowerTexture = Ui.SniperTexture;
    }
}