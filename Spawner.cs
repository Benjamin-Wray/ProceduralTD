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
    private const int MinSpawnRange = 300;
    private const int MaxSpawnRange = 400;
    
    private readonly Texture2D[] _frames = WaveManager.PortalFrames;
    private int _currentFrame;
    private float _timer;
    private const float NextFrameTime = 200;

    private List<Point> _shortestPath = new();
    private readonly Task _pathGenerated;

    internal Spawner()
    {
        SetPosition();
        _pathGenerated = Task.Run(FindShortestPath);
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

    private void FindShortestPath()
    {
        _shortestPath = JumpPointSearch(_position, Player.Castle.Position);
    }

    private List<Point> _visited;
    private List<Point> JumpPointSearch(Point startPoint, Point endPoint)
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

            CheckConnections(currentPoint, endPoint, ref unvisited, ref _visited, ref parentPoints, ref gScore, ref fScore);

            _visited.Add(currentPoint);
            
            if (currentPoint == endPoint) break;
        }

        BuildShortestPath(parentPoints, endPoint, ref shortestPath);
        shortestPath.Reverse();

        return shortestPath;
    }

    private static void CheckConnection(Point connection, Point parent, Point endPoint, ref List<Point> unvisited, ref List<Point> visited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore)
    {
        if (visited.Contains(connection)) return;
        
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

    private void CheckConnections(Point currentPoint, Point endPoint, ref List<Point> unvisited, ref List<Point> visited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore)
    {
        Point[] directions = {new(0, 1), new(1, 0), new(0, -1), new(-1, 0), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)};
        foreach (var direction in directions)
        {
            if (direction.X == 0 || direction.Y == 0) Scan(currentPoint, direction, 1, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore);
            else DiagonalScan(currentPoint, direction, 1, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore);
        }
    }

    private bool Scan(Point parent, Point direction, int distance, Point endPoint, ref List<Point> unvisited, ref List<Point> visited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore, bool fromDiagonal = false)
    {
        Point x = parent + new Point(direction.X * distance, direction.Y * distance);

        if (!MapGenerator.MapBounds.Contains(x + direction) || IsPointWater(x + direction)) return false;
        if (!MapGenerator.MapBounds.Contains(x + direction + direction) || IsPointWater(x + direction + direction)) return false;
        
        Point a = x + new Point(direction.Y, direction.X);
        Point b = x + new Point(-direction.Y, -direction.X);
        
        if (x == endPoint || (IsPointWater(a) && IsPointNotWater(a + direction)) || (IsPointWater(b) && IsPointNotWater(b + direction)))
        {
            if (fromDiagonal) return true;
            
            CheckConnection(x, parent, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore);
            return false;
        }

        return Scan(parent, direction, ++distance, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore, fromDiagonal);
    }

    private void DiagonalScan(Point parent, Point direction, int distance, Point endPoint, ref List<Point> unvisited, ref List<Point> visited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore)
    {
        Point x = parent + new Point(direction.X * distance, direction.Y * distance);
        
        if (!MapGenerator.MapBounds.Contains(x) || IsPointWater(x)) return;

        bool a = Scan(x, new Point(0, direction.Y), 1, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore, true);
        bool b = Scan(x, new Point(direction.X, 0), 1, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore, true);

        if (x == endPoint || a || b)
        {
            CheckConnection(x, parent, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore);
            return;
        }
        
        DiagonalScan(parent, direction, ++distance, endPoint, ref unvisited, ref visited, ref parentPoints, ref gScore, ref fScore);
    }

    private static bool IsPointWater(Point p) => MapGenerator.MapBounds.Contains(p) && MapGenerator.NoiseMap[p.X, p.Y] <= MapGenerator.WaterLevel;
    private static bool IsPointNotWater(Point p) => MapGenerator.MapBounds.Contains(p) && MapGenerator.NoiseMap[p.X, p.Y] > MapGenerator.WaterLevel;
    
    private static float OctileDistance(Point currentPoint, Point endPoint)
    {
        float dx = Math.Abs(currentPoint.X - endPoint.X);
        float dy = Math.Abs(currentPoint.Y - endPoint.Y);
        float distance = dx + dy + (float) (Math.Sqrt(2) - 2) * Math.Min(dx, dy);

        return distance;
    }

    private static void BuildShortestPath(Dictionary<Point, Point> parentPoints, Point point, ref List<Point> shortestPath)
    {
        shortestPath.Add(point);
        
        if (!parentPoints.ContainsKey(point)) return;
        Point parentPoint = parentPoints[point];
        
        Point direction = parentPoint - point;
        if (direction.X != 0) direction.X /= Math.Abs(direction.X);
        if (direction.Y != 0) direction.Y /= Math.Abs(direction.Y);

        Point currentPoint = point + direction;
        while (currentPoint != parentPoint)
        {
            shortestPath.Add(currentPoint);
            currentPoint += direction;
        }

        BuildShortestPath(parentPoints, parentPoint, ref shortestPath);
    }
    
    internal void Update(GameTime gameTime)
    {
        Ui.PlayAnimation(gameTime, ref _timer, NextFrameTime, ref _currentFrame, _frames.Length);
    }
    
    internal void Draw()
    {
        Main.SpriteBatch.Draw(_frames[_currentFrame], _position.ToVector2(), null, Color.White, 0f, _frames[_currentFrame].Bounds.Center.ToVector2(), 1f, SpriteEffects.None, 0f);
        if (_pathGenerated.IsCompleted)
        {
            foreach (Point point in _visited) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), Color.DarkOrange);
            foreach (Point point in _shortestPath) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), Color.Fuchsia);
        }
    }
}