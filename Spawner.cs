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
    private const int MaxSpawnRange = 300;
    
    private readonly Texture2D[] _frames = WaveManager.PortalFrames;
    private int _currentFrame;
    private float _timer;
    private const float NextFrameTime = 200;

    private List<Point> _shortestPath = new();

    internal Spawner()
    {
        SetPosition();
        AStar();
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
            newPosition = (new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * r).ToPoint() + Player.Castle.Position;
            topLeftPosition = newPosition - (_frames[_currentFrame].Bounds.Size.ToVector2() / 2).ToPoint();

            //check if it is on the map
            if (MapGenerator.MapBounds.Contains(new Rectangle(topLeftPosition, texture.Bounds.Size)))
            {
                //check if it on water or a tower
                Parallel.For(0, texture.Height, y =>
                {
                    Parallel.For(0, texture.Width, x =>
                    {
                        Point checkPosition = new Point(x, y) + topLeftPosition;
                        if (Tower.IsInRadius(checkPosition, newPosition, texture.Width / 2f))
                        {
                            if (TowerPlacement.InvalidPositions[checkPosition.X, checkPosition.Y]) isPositionValid = false;
                        }
                    });
                });
            }
            else isPositionValid = false;
        }
        
        _position = newPosition;
        
        Tower.UpdateSpaceValidity(true, texture, _position, topLeftPosition);
    }

    private Point _startPoint;
    private readonly Point _endPoint = Player.Castle.Position;
    private List<Point> _visited;
    
    private void AStar()
    {
        _startPoint = _position;
        
        List<Point> unvisited = new List<Point> {_startPoint};
        _visited = new List<Point>();
        
        Dictionary<Point, float> gScore = new Dictionary<Point, float> {{_startPoint, 0}};
        Dictionary<Point, float> fScore = new Dictionary<Point, float> {{_startPoint, 0}};
        Dictionary<Point, Point> parentPoints = new Dictionary<Point, Point>();
        
        while (unvisited.Count > 0)
        {
            unvisited = unvisited.OrderBy(x => fScore[x]).ThenBy(x => gScore[x]).ToList();
            
            Point currentPoint = unvisited[0];
            unvisited.RemoveAt(0);
            
            foreach (Point connection in GetConnections(currentPoint))
            {
                if (_visited.Contains(connection)) continue;
                
                float cost = CalculateCost(currentPoint, connection);
                if (!gScore.ContainsKey(connection))
                {
                    parentPoints.Add(connection, currentPoint);
                    gScore.Add(connection, gScore[currentPoint] + cost);
                    fScore.Add(connection, gScore[currentPoint] + CalculateHScore(currentPoint, _endPoint));
                }
                else if (gScore[currentPoint] + cost < gScore[connection])
                {
                    parentPoints[connection] = currentPoint;
                    gScore[connection] = gScore[currentPoint] + cost;
                    fScore[connection] = gScore[currentPoint] + CalculateHScore(currentPoint, _endPoint);
                }

                if (!unvisited.Contains(connection)) unvisited.Add(connection);
            }
            
            _visited.Add(currentPoint);
            
            if (currentPoint == _endPoint) break;
        }

        BuildShortestPath(ref parentPoints, _endPoint);
        _shortestPath.Reverse();

        if (unvisited.Count == 0)
        {
            WaveManager.Spawners.Remove(this);
            WaveManager.Spawners.Add(new Spawner());
        }
    }
    
    private List<Point> GetConnections(Point currentPoint)
    {
        List<Point> connections = new List<Point>();
        Point[] directions = {new(0, 1), new(1, 0), new(0, -1), new(-1, 0), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)};

        foreach (Point direction in directions)
        {
            Point newConnection = currentPoint + direction;
            if (!MapGenerator.MapBounds.Contains(newConnection)) continue;
            if (TowerPlacement.InvalidPositions[newConnection.X, newConnection.Y])
            {
                Rectangle castle = new Rectangle(Player.Castle.Position - (Player.Castle.BaseTexture.Bounds.Size.ToVector2() / 2).ToPoint(), Player.Castle.BaseTexture.Bounds.Size);
                if (!Tower.IsInRadius(newConnection, _position, _frames[_currentFrame].Width / 2f) && !castle.Contains(newConnection)) continue;
            }
            connections.Add(newConnection);
        }
        return connections;
    }

    private float CalculateCost(Point currentPoint, Point connection)
    {
        float distance = Vector2.Distance(currentPoint.ToVector2(), connection.ToVector2());
            
        float currentPointHeight = MapGenerator.NoiseMap[currentPoint.X, currentPoint.Y];
        float connectionHeight = MapGenerator.NoiseMap[connection.X, connection.Y];
        float heightDifference = connectionHeight - currentPointHeight;

        return distance + heightDifference;
    }

    private float CalculateHScore(Point currentPoint, Point endPoint)
    {
        float dx = Math.Abs(currentPoint.X - endPoint.X);
        float dy = Math.Abs(currentPoint.Y - endPoint.Y);
        float distance = dx + dy + (float) (Math.Sqrt(2) - 2) * Math.Min(dx, dy);
        
        float currentPointHeight = MapGenerator.NoiseMap[currentPoint.X, currentPoint.Y];
        float connectionHeight = MapGenerator.NoiseMap[endPoint.X, endPoint.Y];
        float heightDifference = connectionHeight - currentPointHeight;

        return distance + heightDifference;
    }

    private void BuildShortestPath(ref Dictionary<Point, Point> parentPoints, Point point)
    {
        if (!parentPoints.ContainsKey(point)) return;
        
        _shortestPath.Add(point);
        BuildShortestPath(ref parentPoints, parentPoints[point]);
    }
    
    internal void Update(GameTime gameTime)
    {
        Ui.PlayAnimation(gameTime, ref _timer, NextFrameTime, ref _currentFrame, _frames.Length);
    }
    
    internal void Draw()
    {
        Main.SpriteBatch.Draw(_frames[_currentFrame], _position.ToVector2(), null, Color.White, 0f, _frames[_currentFrame].Bounds.Center.ToVector2(), 1f, SpriteEffects.None, 0f);
        if (_shortestPath.Count > 0) foreach (Point point in _visited) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), new Color(Color.MidnightBlue, 100));
        if (_shortestPath.Count > 0) foreach (Point point in _shortestPath) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), Color.Crimson);
    }
}