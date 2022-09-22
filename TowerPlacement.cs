using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class TowerPlacement
{
    internal static readonly RenderTarget2D TowerTarget = new(Main.Graphics.GraphicsDevice, Camera.CameraWidth, Camera.CameraHeight);

    internal static readonly bool[,] InvalidPositions = new bool[MapGenerator.MapWidth, MapGenerator.MapHeight];
    
    private static List<Tower> _towers = new();
    private static Tower? _selectedTower;
    private static Castle _castle;

    private static bool _isLeftMouseDown;
    private static bool _canPlaceTower;
    
    internal static void Update()
    {
        MouseState mouseState = Mouse.GetState();

        SelectTower();
        CheckIfPositionIsValid();
        
        switch (mouseState.LeftButton, _isLeftMouseDown)
        {
            case (ButtonState.Pressed, false):
            {
                _isLeftMouseDown = true;
                PlaceTower();
                break;
            }
            case (ButtonState.Released, true):
                _isLeftMouseDown = false;
                break;
        }
    }

    private static void SelectTower()
    {
        if (Ui.Selected is < 4)
        {
            Point mousePosition = WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds);
            Point towerPosition = mousePosition + Camera.CameraPosition.ToPoint();
            _selectedTower = Ui.Selected.Value switch
            {
                (int)Ui.Towers.LandMine => new LandMine(towerPosition),
                (int)Ui.Towers.Cannon => new Cannon(towerPosition),
                (int)Ui.Towers.NailGun => new NailGun(towerPosition),
                (int)Ui.Towers.Sniper => new Sniper(towerPosition),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        else if (Ui.Selected is (int)Ui.Towers.Upgrade)
        {
            //TODO add upgrade feature
        }
        else if (Ui.Selected is (int)Ui.Towers.Sell)
        {
            //TODO add sell feature
        }
        else
        {
            _selectedTower = null;
        }
    }

    private static void PlaceTower()
    {
        if (_selectedTower != null && _canPlaceTower)
        {
            _towers.Add(_selectedTower);
            _selectedTower.InvalidateSpace();
        }
    }

    private static void CheckIfPositionIsValid()
    {
        if (_selectedTower == null) return;
        
        _canPlaceTower = true;
        Texture2D texture = _selectedTower.TowerTexture[0];
        Point position = Ui.CentrePosition(_selectedTower.Position.ToVector2(), texture).ToPoint();
        for (int y = 0; y < texture.Height; y++)
        {
            for (int x = 0; x < texture.Width; x++)
            {
                if (IsInRange(position + new Point(x, y), _selectedTower.Position, texture.Width / 2f)) continue;
                
                if (!new Rectangle(0, 0, MapGenerator.MapWidth, MapGenerator.MapHeight).Contains(new Point(x + position.X, y + position.Y))) _canPlaceTower = false;
                else if (InvalidPositions[x + position.X, y + position.Y]) _canPlaceTower = false;
            }
        }
    }

    internal static bool IsInRange(Point position, Point origin, float radius)
    {
        return Vector2.Distance(position.ToVector2(), origin.ToVector2()) > radius;
    }

    internal static void Draw()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(TowerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawTowersOnMap();
        
        DrawCursor();
        
        Main.SpriteBatch.End();
    }

    private static void DrawTowersOnMap()
    {
        foreach (Tower tower in _towers)
        {
            tower.DrawTower();
        }
    }

    private static void DrawCursor()
    {
        Point mousePosition = WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds);
        if (Ui.Selected != null && Camera.CameraTarget.Bounds.Contains(mousePosition))
        {
            Color tint = _canPlaceTower ? Ui.CanBuyColour : Ui.CannotBuyColour;
            tint.A = (byte)(tint.A * .8f);
            foreach (var texture in Ui.ButtonDrawOrder[Ui.Selected.Value])
            {
                Main.SpriteBatch.Draw(texture, Ui.CentrePosition(mousePosition.ToVector2(), texture), tint); 
            }
        }
    }
}

internal class Castle : Tower
{
    private int _hp;
    internal int Hp
    {
        get => _hp;
        set => _hp = value < 0 ? 0 : value;
    }

    public Castle(Point position, int hp) : base(position)
    {
        _hp = hp;
    }
}