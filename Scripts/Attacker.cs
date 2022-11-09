using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal class Attacker
{
    internal Point Position;
    private readonly Queue<Point> _path; //path the attacker will follow
    private Point _nextPoint;
    private float _nextPointTime;
    private float _timer;
    private const float Speed = 15;

    private Texture2D _currentTexture;
    internal const int MaxHp = 6; //maximum hp an attacker can have
    private int _hp;

    internal int Hp
    {
        get => _hp;
        set
        {
            if (value <= 0) //if the attacker's hp reaches 0 and "dies"
            {
                if (WaveManager.Attackers.Contains(this)) WaveManager.Attackers.Remove(this); //destroy attacker
                return;
            }
            _hp = Math.Clamp(value, 0, MaxHp); //update hp
            _currentTexture = WaveManager.AttackerColours[_hp - 1]; //update texture
        }
    }

    internal Attacker(int hp, Point position, IEnumerable<Point> shortestPath)
    {
        Hp = hp;
        Position = position;
        _path = new Queue<Point>(shortestPath); //pass in path from spawner
        GetNextPoint();
    }
    
    internal void Update(GameTime gameTime)
    {
        if (_nextPoint == Position) GetNextPoint(); //if at next point, start moving to next point after that
        
        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds; //update timer

        if (_timer >= _nextPointTime) //if sufficient time has passed to move to the next point
        {
            Position = _nextPoint; //move to next point in shortest path
            _timer %= _nextPointTime; //reset timer
        }
    }

    private void GetNextPoint()
    {
        if (!_path.TryDequeue(out _nextPoint)) //attempt to dequeue the next point in the shortest path
        {
            //if the path queue is empty, the attacker has reached the castle
            Player.Health -= Hp;
            WaveManager.Attackers.Remove(this); //destroy the attacker
        }
        _nextPointTime = Spawner.OctileDistance(Position, _nextPoint, false) / Speed; //calculate the time to wait before moving to the next point in the path
    }

    internal void Draw()
    {
        Main.SpriteBatch.Draw(_currentTexture, Position.ToVector2(), null, Color.White, 0, _currentTexture.Bounds.Center.ToVector2(), 1, SpriteEffects.None, 0);
    }
}