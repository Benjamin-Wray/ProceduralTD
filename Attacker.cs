using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal class Attacker
{
    internal Point Position;
    private readonly Queue<Point> _path;
    private Point _nextPoint;
    private float _nextPointTime;
    
    private float _timer;
    private const float Speed = 15;

    internal const int MaxHp = 6;
    private int _hp;
    internal int Hp
    {
        get => _hp;
        set
        {
            if (value <= 0)
            {
                Player.Money += _hp - Math.Clamp(value, 0, MaxHp);
                if (WaveManager.Attackers.Contains(this)) WaveManager.Attackers.Remove(this);
                return;
            }
            _hp = Math.Clamp(value, 0, MaxHp);
            _currentTexture = WaveManager.AttackerColours[_hp - 1];
        }
    }

    private Texture2D _currentTexture;

    internal Attacker(int hp, Point position, IEnumerable<Point> shortestPath)
    {
        Hp = hp;
        Position = position;
        _path = new Queue<Point>(shortestPath);
        GetNextPoint();
    }
    
    internal void Update(GameTime gameTime)
    {
        if (_nextPoint == Position) GetNextPoint();
        
        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timer >= _nextPointTime)
        {
            Position = _nextPoint;
            _timer %= _nextPointTime;
        }
    }

    private void GetNextPoint()
    {
        if (!_path.TryDequeue(out _nextPoint))
        {
            Player.Health -= Hp;
            WaveManager.Attackers.Remove(this);
        }
        _nextPointTime = Spawner.OctileDistance(Position, _nextPoint, false) / Speed;
    }

    internal void Draw()
    {
        Main.SpriteBatch.Draw(_currentTexture, Position.ToVector2(), null, Color.White, 0, _currentTexture.Bounds.Center.ToVector2(), 1, SpriteEffects.None, 0);
    }
}