using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Camera
{
    internal const int CameraWidth = 352;
    internal const int CameraHeight = 288;

    private const int CameraRangeX = MapGenerator.MapWidth - CameraWidth;
    private const int CameraRangeY = MapGenerator.MapHeight - CameraHeight;
    private static Vector2 _cameraPosition = new(CameraRangeX / 2f, CameraRangeY / 2f); //camera initially positioned in the middle of the map;
    
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
                _colourMap[x, y] = MapGenerator.NoiseMap[x, y] switch
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
    }
    
    //called every frame
    internal static void MoveCamera(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        Vector2 direction = Vector2.Zero; //vector that represents the direction the camera will move this frame

        //set direction from keyboard input
        if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D)) direction.X += 1;
        if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A)) direction.X += -1;
        if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S)) direction.Y += 1;
        if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W)) direction.Y += -1;

        //if the direction was changed
        if (direction != Vector2.Zero)
        {
            direction.Normalize(); //make the modulus of the vector 1 so diagonal movement is the same speed as horizontal/vertical movement
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; //the time passed since the last frame
            
            //move the camera 
            Vector2 newCameraPosition = _cameraPosition + direction * MoveSpeed * deltaTime;
            //move horizontally
            _cameraPosition.X = newCameraPosition.X switch
            {
                < 0 => 0,
                > CameraRangeX => CameraRangeX,
                _ => newCameraPosition.X
            };
            //move vertically
            _cameraPosition.Y = newCameraPosition.Y switch
            {
                < 0 => 0,
                > CameraRangeY => CameraRangeY,
                _ => newCameraPosition.Y
            };
        }
    }
    
    internal static void DrawMap() //called every frame after update
    {
        for (int y = 0; y < CameraHeight; y++)
        {
            for (int x = 0; x < CameraWidth; x++)
            {
                Vector2 position = new Vector2(x, y); //position on the screen where the point will be drawn
                Color colour = _colourMap[x + (int)Math.Floor(_cameraPosition.X), y + (int)Math.Floor(_cameraPosition.Y)]; //the colour is selected based on the position on the map
                Main.SpriteBatch.Draw(_pixel, position, colour); //pixel is drawn to the screen
            }
        }
    }
}