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
    
    private readonly Texture2D?[] _frames = WaveManager.PortalFrames;
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
        _generatePath = Task.Run(FindShortestPath); //starts generating the path asynchronously 
        UpdateAttackersToSpawn();
    }

    private void SetPosition()
    {
        Texture2D? texture = _frames[_currentFrame];
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
        
        Tower.UpdateSpaceValidity(true, texture, Tower.GetColoursFromTexture(texture), topLeftPosition);
    }

    private void FindShortestPath() => _shortestPath = AStarSearch(_position, TowerManager.Castle.Position); //calculates the shortest path between the spawner and the castle

    private List<Point> AStarSearch(Point startPoint, Point endPoint)
    {
        List<Point> shortestPath = new List<Point>();
        List<Point> unvisited = new List<Point> {startPoint}; //stores the next points that can be visited from visited points
        _visited = new List<Point>(); //stores the points that have already been visited

        Dictionary<Point, float> gScore = new Dictionary<Point, float> {{startPoint, 0}}; //distance from the start point
        Dictionary<Point, float> fScore = new Dictionary<Point, float> {{startPoint, 0}}; //gScore + heuristic
        Dictionary<Point, Point> parentPoints = new Dictionary<Point, Point>(); //keeps track of where the algorithm got to each point from, used to build shortest path

        while (unvisited.Count > 0) //loops until every reachable node has been visited
        {
            unvisited = unvisited.OrderBy(x => fScore[x]).ThenBy(x => gScore[x]).ToList(); //sort the unvisited list by fScore then gScore 
            
            //set the current point to the first element in the sorted list and remove it
            Point currentPoint = unvisited[0];
            unvisited.RemoveAt(0);

            //check each adjacent point
            CheckConnections(currentPoint, endPoint, ref unvisited, ref _visited, ref parentPoints, ref gScore, ref fScore);

            _visited.Add(currentPoint); //set the point to visited

            if (currentPoint == endPoint) //if we have reached the castle
            {
                //build and return the shortest path
                BuildShortestPath(endPoint, parentPoints, ref shortestPath);
                shortestPath.Reverse();

                return shortestPath;
            }

            if (_visited.Count >= 10000) break;
        }
        
        return shortestPath; //if program exits the while loop without returning a shortest path, there is no possible path between the spawner and the castle
    }

    private void CheckConnections(Point currentPoint, Point endPoint, ref List<Point> unvisited, ref List<Point> visited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore)
    {
        //check each point that is adjacent to the current point
        List<Point> directions = new List<Point> {new(0, 1), new(1, 0), new(0, -1), new(-1, 0), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)};
        foreach (Point direction in directions)
        {
            Point connection = currentPoint + direction;
            
            //ignore points that are out of bounds of the map, on water, or have already been visited
            if (!MapGenerator.MapBounds.Contains(connection)) continue;
            if (MapGenerator.NoiseMap[connection.X, connection.Y] <= MapGenerator.WaterLevel) continue;
            if (visited.Contains(connection)) continue;
            
            CheckConnection(connection, currentPoint, endPoint, ref unvisited, ref parentPoints, ref gScore, ref fScore);
        }
    }

    private void CheckConnection(Point connection, Point parent, Point endPoint, ref List<Point> unvisited, ref Dictionary<Point, Point> parentPoints, ref Dictionary<Point, float> gScore, ref Dictionary<Point, float> fScore)
    {
        float cost = OctileDistance(parent, connection); //get cost to move from the current point to the connected point
        float newGScore = gScore[parent] + cost;
        if (!gScore.ContainsKey(connection))
        {
            //add connected point to parentPoints, gScore and fScore
            parentPoints.Add(connection, parent);
            gScore.Add(connection, newGScore);
            fScore.Add(connection, newGScore + OctileDistance(parent, endPoint));
        }
        else if (gScore[parent] + cost < gScore[connection])
        {
            //replace parentPoints, gScore and fScore with new values
            parentPoints[connection] = parent;
            gScore[connection] = newGScore;
            fScore[connection] = newGScore + OctileDistance(parent, endPoint);
        }

        if (!unvisited.Contains(connection)) unvisited.Add(connection);
    }

    internal static float OctileDistance(Point currentPoint, Point endPoint, bool heuristic = true)
    {
        //calculate octile distance between both points
        float dx = Math.Abs(currentPoint.X - endPoint.X);
        float dy = Math.Abs(currentPoint.Y - endPoint.Y);
        float distance = dx + dy + (float)(Math.Sqrt(2) - 2) * Math.Min(dx, dy);
        
        //get difference in heights between points, height difference has a greater influence on the heuristic than the cost
        float heightDifference = (MapGenerator.NoiseMap[endPoint.X, endPoint.Y] - MapGenerator.NoiseMap[currentPoint.X, currentPoint.Y]) * (heuristic ? 100f : 50);

        return distance + heightDifference;
    }
    
    //recursively works backwards from the end to the start using the parentPoints dictionary, adding each parent to the shortest path 
    private static void BuildShortestPath(Point point, Dictionary<Point, Point> parentPoints, ref List<Point> shortestPath)
    {
        shortestPath.Add(point);
        
        if (!parentPoints.ContainsKey(point)) return; //if the current point does not have a parent, we have reached the start

        BuildShortestPath(parentPoints[point], parentPoints, ref shortestPath); //add the parent of the parent point to the shortest path
    }
    
    internal void Update(GameTime gameTime)
    {
        if (_generatePath.IsCompleted && _shortestPath.Count == 0) //if a shortest path was not found
        {
            WaveManager.Spawners.Add(new Spawner()); //create a new spawner
            WaveManager.Spawners.Remove(this); //destroy the spawner
        }
        
        //only spawn attackers when every spawner's path has been completed, this means the spawners will begin spawning attackers at the same time
        if (WaveManager.Spawners.All(x => x._generatePath.IsCompleted)) SpawnAttacker(gameTime);
        
        Ui.PlayAnimation(gameTime, ref _animationTimer, NextFrameTime, ref _currentFrame, _frames.Length); //update idle animation
    }

    private void SpawnAttacker(GameTime gameTime)
    {
        if (!CanSpawn) return;
        
        _spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_spawnTimer >= TimeToSpawn)
        {
            WaveManager.Attackers.Add(new Attacker(_attackersToSpawn[0][0], _position, _shortestPath)); //get attacker at first position in list
            _attackersToSpawn[0][1]--; //decrement attacker count
            if (_attackersToSpawn[0][1] <= 0) _attackersToSpawn.RemoveAt(0); //if attacker count is zero, remove it from the list
            if (_attackersToSpawn.Count == 0) //if there are no attackers left to spawn
            {
                CanSpawn = false;
                _spawnTimer = 0;
            }
            _spawnTimer %= TimeToSpawn;
        }
    }

    internal void UpdateAttackersToSpawn()
    {
        //calculate number of attackers to spawn
        int number = (WaveManager.CurrentWave / Attacker.MaxHp + 1) * 2 + 5;
        int maxHp = WaveManager.CurrentWave % Attacker.MaxHp;
        if (maxHp == 0) maxHp += Attacker.MaxHp;

        int hp = 1;
        while (hp <= maxHp) _attackersToSpawn.Add(new[] {hp++, number}); //add attackers to list
        
        CanSpawn = true;
    }
    
    internal void Draw()
    {
        if (!_generatePath.IsCompleted) return;
        
        Main.SpriteBatch.Draw(_frames[_currentFrame], _position.ToVector2(), null, Color.White, 0f, _frames[_currentFrame].Bounds.Center.ToVector2(), 1f, SpriteEffects.None, 0f);
        foreach (Point point in _visited) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), new Color(Color.DarkOrange, 10));
        foreach (Point point in _shortestPath) Main.SpriteBatch.Draw(WaveManager.Pixel, point.ToVector2(), Color.SaddleBrown);
    }
}