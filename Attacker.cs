using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal class Attacker
{
    private Point _position;
    private readonly Queue<Point> _path;
    private Point _nextPoint;
    private float _nextPointTime;
    
    private float _timer;
    private const float Speed = 10;

    internal const int MaxHp = 6;
    private int _hp;

    private int Hp
    {
        get => _hp;
        set
        {
            _hp = Math.Clamp(value, 0, MaxHp);
            if (_hp == 0)
            {
                Player.Health -= Hp;
                WaveManager.Attackers.Remove(this);
                return;
            }
            _currentTexture = WaveManager.AttackerColours[_hp - 1];
        }
    }

    private Texture2D _currentTexture;

    internal Attacker(int hp, Point position, IEnumerable<Point> shortestPath)
    {
        Hp = hp;
        _position = position;
        _path = new Queue<Point>(shortestPath);
        GetNextPoint();
    }
    
    internal void Update(GameTime gameTime)
    {
        if (_nextPoint == _position) GetNextPoint();
        
        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timer >= _nextPointTime)
        {
            _position = _nextPoint;
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
        _nextPointTime = Spawner.OctileDistance(_position, _nextPoint) / Speed;
    }

    internal void Draw()
    {
        Main.SpriteBatch.Draw(_currentTexture, _position.ToVector2(), null, Color.White, 0, _currentTexture.Bounds.Center.ToVector2(), 1, SpriteEffects.None, 0);
    }
}