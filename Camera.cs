using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Camera
{
    internal static readonly RenderTarget2D CameraTarget = new(Main.Graphics.GraphicsDevice, CameraWidth, CameraHeight);
    
    internal const int CameraWidth = 352;
    internal const int CameraHeight = 288;

    private const int CameraRangeX = MapGenerator.MapWidth - CameraWidth;
    private const int CameraRangeY = MapGenerator.MapHeight - CameraHeight;
    internal static Vector2 CameraPosition = new(CameraRangeX / 2f, CameraRangeY / 2f); //camera initially positioned in the middle of the map;

    private const float WaterLevel = .4f;
    private static readonly Dictionary<float, Color> MapColors = new()
    {
        {WaterLevel, Color.Blue},
        {.45f, Color.LightYellow},
        {.6f, Color.ForestGreen},
        {.75f, Color.Green},
        {.85f, Color.Gray},
        {.95f, Color.DarkGray},
        {1f, Color.Snow},
    };

    private static readonly Dictionary<Keys, Vector2> MovementKeys = new()
    {
        {Keys.W, new Vector2(0, -1)},
        {Keys.Up, new Vector2(0, -1)},
        {Keys.A, new Vector2(-1, 0)},
        {Keys.Left, new Vector2(-1, 0)},
        {Keys.S, new Vector2(0, 1)},
        {Keys.Down, new Vector2(0, 1)},
        {Keys.D, new Vector2(1, 0)},
        {Keys.Right, new Vector2(1, 0)}
    };

    private const float MoveSpeed = 200f; //how fast the camera moves
    private static Color[,] _colourMap;

    private static Texture2D _pixel;
    
    internal static void LoadContent()
    {
        _pixel = Main.ContentManager.Load<Texture2D>("images/map/pixel");
    }   

    internal static void GenerateColourMap() //generate 2D array of colours to be drawn to the screen to represent height
    {
        _colourMap = new Color[MapGenerator.MapWidth, MapGenerator.MapHeight]; //create 2d array with the same size as the heightmap

        for (int y = 0; y < MapGenerator.MapHeight; y++)
        {
            for (int x = 0; x < MapGenerator.MapWidth; x++)
            {
                //assign colours based on the height of each point on the heightmap
                foreach (KeyValuePair<float,Color> pair in MapColors)
                    if (MapGenerator.NoiseMap[x, y] <= pair.Key)
                    {
                        _colourMap[x, y] = pair.Value;
                        break;
                    }
                if (MapGenerator.NoiseMap[x, y] <= WaterLevel) TowerPlacement.InvalidPositions[x, y] = true;
            };
        };
    }
    
    //called every frame
    internal static void MoveCamera(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        Vector2 direction = Vector2.Zero; //vector that represents the direction the camera will move this frame
        
        //set direction from keyboard input
        foreach(Keys key in keyboardState.GetPressedKeys()) if (MovementKeys.ContainsKey(key)) direction += MovementKeys[key];
        
        //if the direction was changed
        if (direction != Vector2.Zero)
        {
            direction.Normalize(); //make the modulus of the vector 1 so diagonal movement is the same speed as horizontal/vertical movement
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; //the time passed since the last frame
            
            //move the camera 
            Vector2 newCameraPosition = CameraPosition + direction * MoveSpeed * deltaTime;
            
            //move horizontally
            CameraPosition.X = newCameraPosition.X switch
            {
                < 0 => 0,
                > CameraRangeX => CameraRangeX,
                _ => newCameraPosition.X
            };
            //move vertically
            CameraPosition.Y = newCameraPosition.Y switch
            {
                < 0 => 0,
                > CameraRangeY => CameraRangeY,
                _ => newCameraPosition.Y
            };
        }
    }
    
    internal static void DrawMap() //called every frame after update
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(CameraTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        
        Main.SpriteBatch.Begin();
        
        for (int y = 0; y < CameraHeight; y++)
        {
            for (int x = 0; x < CameraWidth; x++)
            {
                Vector2 position = new Vector2(x, y); //position on the screen where the point will be drawn
                Color colour = _colourMap[x + (int)Math.Floor(CameraPosition.X), y + (int)Math.Floor(CameraPosition.Y)]; //the colour is selected based on the position on the map
                Main.SpriteBatch.Draw(_pixel, position, colour); //pixel is drawn to the screen
            }
        }
        
        Main.SpriteBatch.End();
    }
}