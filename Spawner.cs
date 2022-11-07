using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal class Spawner
{
    private Point _position = Point.Zero;
    private const int MinSpawnRange = 200;
    private const int MaxSpawnRange = 400;
    
    private readonly Texture2D[] _frames = WaveManager.PortalFrames;
    private int _currentFrame;
    private float _animationTimer;
    private float _spawnTimer;
    private const float NextFrameTime = .2f;
    private const float TimeToSpawn = 1;

    private List<Point> _shortestPath = new();
    private List<Point> _visited;
    private readonly Task _generatePath;

    private readonly List<int[]> _attackersToSpawn = new();
    internal bool CanSpawn;

    internal Spawner()
    {
        SetPosition();
        _generatePath = Task.Run(FindShortestPath);
        UpdateAttackersToSpawn();
    }

    private void SetPosition()
    {
        Texture2D texture = _frames[_currentFrame];
        Random random = new Random();
        Point newPosition = Point.Zero;
        Point topLeftPosition = Point.Zero;
        
        bool isPositionValid = false;
        while (!isPositionValid)
        {
            isPositionValid = true;
            
            //generate polar coordinates
            float r = random.Next(MinSpawnRange, MaxSpawnRange); //generates a random distance within a specified range from the castle
            float angle = random.Next(0, 3600) / 10f; //generates a random angle between 0.0 and 360.0
            
            //convert to cartesian coordinates and round them to integers
            newPosition = (new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * r).ToPoint() + TowerManager.Castle.Position;
            topLeftPosition = newPosition - (_frames[_currentFrame].Bounds.Size.ToVector2() / 2).ToPoint();

            //check if it is on the map
            if (MapGenerator.MapBounds.Contains(new Rectangle(topLeftPosition, texture.Bounds.Size)))
            {
                //check if it on water or a tower
                for (int y = 0; y < texture.Height; y++)
                {
                    for (int x = 0; x < texture.Width; x++)
                    {
                        Point checkPosition = new Point(x, y) + topLeftPosition;
                        if (Tower.IsInRadius(checkPosition, newPosition, texture.Width / 2f))
                        {
                            if (TowerManager.InvalidPositions[checkPosition.X, checkPosition.Y]) isPositionValid = false;
                        }
                    }
                }
            }
            else isPositionValid = false;
        }
        
        _position = newPosition;
        
        Tower.UpdateSpaceValidity(true, texture, _position, topLeftPosition);
    }

    private void FindShortestPath()
    {
        _shortestPath = AStarSearch(_position, TowerManager.Castle.Position);
    }

    private List<Point> AStarSearch(Point startPoint, Point endPoint)
    {
        List<Point> shortestPath = new List<Point>();
        List<Point> unvisited = new List<Point> {startPoint};
        _visited = new List<Point>();

        Dictionary<Point, float> gScore = new Dictionary<Point, float> {{startPoint, 0}};
        Dictionary<Point, float> fScore = new Dictionary<Point, float> {{startPoint, 0}};
        Dictionary<Point, Point> parentPoints = new Dictionary<Point, Point>();

        while (unvisited.Count > 0)
        {
            unvisited = unvisited.OrderBy(x => fScore[x]).ThenBy(x => gScore[x]).ToList();
            
            Point currentPoint = unvisited[0];
            unvisited.RemoveAt(0);

            CheckConnectionsAStar(currentPoint, endPoint, ref unvisited, ref _visited, ref parentPoints, ref gScore, ref fScore);

            _visited.Add(currentPoint);

            if (currentPoint == endPoint)
            {
                BuildShortestPath(endPoint, parentPoints, ref shortestPath);
                shortestPath.Reverse();

                return shortestPath;
            }

            if (_visited.Count > 5000) break;
        }

        WaveManager.Spawners.Remove(this);
        WaveManager.Spawners.Add(new Spawner());
        return new List<Point>();
    }

    private static void CheckConnection(Point connection, Point parent, Point endPoint, ref List<Point> unvisited, ref List<Point> visited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore)
    {
        if (visited.Contains(connection)) return;

        Point direction = connection - parent;
        if (direction.X != 0) direction.X /= Math.Abs(direction.X);
        if (direction.Y != 0) direction.Y /= Math.Abs(direction.Y);
        
        float cost = OctileDistance(parent, connection);
        if (!gScore.ContainsKey(connection))
        {
            parentPoints.Add(connection, parent);
            gScore.Add(connection, gScore[parent] + cost);
            fScore.Add(connection, gScore[parent] + OctileDistance(parent, endPoint));
        }
        else if (gScore[parent] + cost < gScore[connection])
        {
            parentPoints[connection] = parent;
            gScore[connection] = gScore[parent] + cost;
            fScore[connection] = gScore[parent] + OctileDistance(parent, endPoint);
        }

        if (!unvisited.Contains(connection)) unvisited.Add(connection);
    }

    private void CheckConnectionsAStar(Point currentPoint, Point endPoint, ref List<Point> unvisited, ref List<Point> visited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore)
    {
        List<Point> directions = new List<Point> {new(0, 1), new(1, 0), new(0, -1), new(-1, 0), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)};
        foreach (Point direction in directions)
        {
            Point connection = currentPoint + direction;
            if (!MapGenerator.MapBounds.Contains(connection)) continue;
            if (MapGenerator.NoiseMap[connection.X, connection.Y] <= MapGenerator.WaterLevel) continue;
            CheckConnection(connection, currentPoint, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore);
        }
    }

    internal static float OctileDistance(Point currentPoint, Point endPoint, bool heuristic = true)
    {
        float dx = Math.Abs(currentPoint.X - endPoint.X);
        float dy = Math.Abs(currentPoint.Y - endPoint.Y);
        float distance = dx + dy + (float)(Math.Sqrt(2) - 2) * Math.Min(dx, dy);
        float heightDifference = MapGenerator.NoiseMap[endPoint.X, endPoint.Y] - MapGenerator.NoiseMap[currentPoint.X, currentPoint.Y];

        return distance + heightDifference * (heuristic ? 100 : 50);
    }
    
    private static void BuildShortestPath(Point point, Dictionary<Point, Point> parentPoints, ref List<Point> shortestPath)
    {
        shortestPath.Add(point);
        
        if (!parentPoints.ContainsKey(point)) return;
        Point parentPoint = parentPoints[point];

        BuildShortestPath(parentPoint, parentPoints, ref shortestPath);
    }
    
    internal void Update(GameTime gameTime)
    {
        if (_generatePath.IsCompleted && WaveManager.Spawners.All(x => x.CanSpawn)) SpawnAttacker(gameTime);
        
        Ui.PlayAnimation(gameTime, ref _animationTimer, NextFrameTime, ref _currentFrame, _frames.Length);
    }

    private void SpawnAttacker(GameTime gameTime)
    {
        if (!CanSpawn) return;
        
        _spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_spawnTimer >= TimeToSpawn)
        {
            WaveManager.Attackers.Add(new Attacker(_attackersToSpawn[0][0], _position, _shortestPath));
            _attackersToSpawn[0][1]--;
            if (_attackersToSpawn[0][1] <= 0) _attackersToSpawn.RemoveAt(0);
            if (_attackersToSpawn.Count == 0)
            {
                CanSpawn = false;
                _spawnTimer = 0;
            }
            _spawnTimer %= TimeToSpawn;
        }
    }

    internal void UpdateAttackersToSpawn()
    {
        int currentWave = WaveManager.CurrentWave;
        
        int number = (currentWave / Attacker.MaxHp + 1) * 2 + 5;
        
        int maxHp = currentWave % Attacker.MaxHp;
        if (maxHp == 0) maxHp += Attacker.MaxHp;

        int hp = 1;
        while (hp <= maxHp)
        {
            _attackersToSpawn.Add(new[] {hp++, number});
        }
        
        CanSpawn = true;
    }
    
    internal void Draw()
    {
        if (!_generatePath.IsCompleted) return;
        
        Main.SpriteBatch.Draw(_frames[_currentFrame], _position.ToVector2(), null, Color.White, 0f, _frames[_currentFrame].Bounds.Center.ToVector2(), 1f, SpriteEffects.None, 0f);
        //foreach (Point point in _visited) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), new Color(Color.DarkOrange, 100));
        foreach (Point point in _shortestPath) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), Color.SaddleBrown);
    }
}