using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Camera
{
    internal const int CameraWidth = 352;
    internal const int CameraHeight = 288;

    private static float _cameraRangeX;
    private static float _cameraRangeY;
    private static Vector2 _cameraPosition;
    
    private const float MoveSpeed = 4f; //how fast the camera moves
    private static Color[,] _colourMap;
    
    //called at start of program
    internal static void Initialize(float[,] heightMap)
    {
        //dimensions of area camera can move
        _cameraRangeX = MapGenerator.MapWidth - CameraWidth;
        _cameraRangeY = MapGenerator.MapHeight - CameraHeight;
        
        _cameraPosition = new Vector2(_cameraRangeX / 2f, _cameraRangeY / 2f); //camera initially positioned in the middle of the map

        _colourMap = GenerateColourMap(heightMap);
    }

    private static Color[,] GenerateColourMap(float[,] heightMap) //generate 2D array of colours to be drawn to the screen to represent height
    {
        Color[,] colourMap = new Color[MapGenerator.MapWidth, MapGenerator.MapHeight]; //create 2d array with the same size as the heightmap
        
        for (int y = 0; y < MapGenerator.MapHeight; y++)
        {
            for (int x = 0; x < MapGenerator.MapWidth; x++)
            {
                //assign colours based on the height of each point on the heightmap
                colourMap[x, y] = heightMap[x, y] switch
                {
                    < 0.4f => Color.Blue,
                    < 0.45f => Color.LightYellow,
                    < 0.6f => Color.ForestGreen,
                    < 0.75f => Color.Green,
                    < 0.85f => Color.Gray,
                    < 0.95f => Color.DarkGray,
                    _ => Color.Snow
                };
            }
        }

        return colourMap;
    }
    
    //called every frame
    internal static void Update()
    {
        KeyboardState keyState = Main.KeyState;
        Vector2 direction = Vector2.Zero; //vector that represents the direction the camera will move this frame

        //set direction from keyboard input
        if ((keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D)) && _cameraPosition.X + MoveSpeed <= _cameraRangeX) direction.X += 1;
        if ((keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A)) && _cameraPosition.X - MoveSpeed >= 0) direction.X += -1;
        if ((keyState.IsKeyDown(Keys.Down) || keyState.IsKeyDown(Keys.S)) && _cameraPosition.Y + MoveSpeed <= _cameraRangeY) direction.Y += 1;
        if ((keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W)) && _cameraPosition.Y - MoveSpeed >= 0) direction.Y += -1;

        //if a direction has been set
        if (direction != Vector2.Zero)
        {
            direction.Normalize(); //make the modulus of the vector 1 so diagonal movement is the same speed as horizontal/vertical movement
            _cameraPosition += direction * MoveSpeed; //multiply movement speed by the direction vector and add it to the camera's position
        }
    }
    
    internal static void DrawMap() //called every frame after update
    {
        for (int y = 0; y < CameraHeight; y++)
        {
            for (int x = 0; x < CameraWidth; x++)
            {
                Vector2 position = new Vector2(x, y); //position on the screen where the point will be drawn
                Color colour = _colourMap[x + (int)_cameraPosition.X, y + (int)_cameraPosition.Y]; //the colour is selected based on the position on the map
                Main.SpriteBatch.Draw(Main.Pixel, position, colour); //pixel is drawn to the screen 
            }
        }
    }
}