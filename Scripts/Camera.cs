using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Camera
{
    internal const int CameraWidth = Ui.UiWidth * 2/3; //the camera's width is two thirds of the ui
    internal const int CameraHeight = Ui.UiHeight; //the camera's height is the same as the ui
    
    private static Texture2D _mapTexture;
    internal static readonly RenderTarget2D CameraTarget = new(Main.Graphics.GraphicsDevice, WindowManager.Scene.Width  * 2/3, WindowManager.Scene.Height);
    internal static readonly float CameraScale = WindowManager.Scene.Height / (float)CameraHeight;
    
    private static readonly Vector2 CameraRange = new(MapGenerator.MapWidth - CameraWidth, MapGenerator.MapHeight - CameraHeight); //area the camera can move in
    internal static Vector2 CameraPosition = CameraRange / -2; //camera initially positioned in the middle of the map;
    
    private static readonly Dictionary<float, Color> MapColors = new()
    {
        {MapGenerator.WaterLevel, Color.Blue},
        {.45f, Color.LightYellow},
        {.6f, Color.ForestGreen},
        {.75f, Color.Green},
        {.85f, Color.Gray},
        {.95f, Color.DarkGray},
        {1f, Color.Snow}
    }; //dictionary of which colours represent which height levels

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
    }; //dictionary of keys and corresponding direction vectors

    private const float MoveSpeed = 200; //how fast the camera moves

    internal static void GenerateMapTexture() //generate 2D array of colours to be drawn to the screen to represent height
    {
        _mapTexture = new Texture2D(Main.Graphics.GraphicsDevice, MapGenerator.MapWidth, MapGenerator.MapHeight); //create empty texture for map
        TowerManager.InvalidPositions = new bool[MapGenerator.MapWidth, MapGenerator.MapHeight];

        Color[] colourMap = new Color[_mapTexture.Width * _mapTexture.Height]; //stores the colour of each pixel in the map texture

        //iterate through each value in the heightmap
        Parallel.For(0, _mapTexture.Height, y =>
        {
            Parallel.For(0, _mapTexture.Width, x =>
            {
                //assign colours based on the height of each point on the heightmap
                foreach (KeyValuePair<float, Color> pair in MapColors)
                {
                    if (MapGenerator.NoiseMap[x, y] <= pair.Key)
                    {
                        colourMap[x % _mapTexture.Width + y * _mapTexture.Width] = pair.Value;
                        break;
                    }
                }

                if (MapGenerator.NoiseMap[x, y] <= MapGenerator.WaterLevel) TowerManager.InvalidPositions[x, y] = true; //towers cannot be placed on water
            });
        });
        
        _mapTexture.SetData(colourMap); //assign colours to map texture
    }
    
    //called every frame
    internal static void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        Vector2 direction = Vector2.Zero; //vector that represents the direction the camera will move this frame
        
        //set direction from keyboard input
        foreach(Keys key in keyboardState.GetPressedKeys()) if (MovementKeys.ContainsKey(key)) direction -= MovementKeys[key];
        direction = new Vector2(Math.Clamp(direction.X, -1, 1), Math.Clamp(direction.Y, -1, 1));
        
        //if the direction was changed
        if (direction != Vector2.Zero)
        {
            direction.Normalize(); //make the modulus of the vector 1 so diagonal movement is the same speed as horizontal/vertical movement
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; //the time passed since the last frame
            
            //move the camera 
            Vector2 newCameraPosition = CameraPosition + direction * MoveSpeed * deltaTime;

            CameraPosition = new Vector2(Math.Clamp(newCameraPosition.X, -CameraRange.X, 0), Math.Clamp(newCameraPosition.Y, -CameraRange.Y, 0));
        }
    }
    
    internal static void DrawMap() //called every frame after update
    {
        //draw spawners, attackers and towers
        WaveManager.Draw();
        TowerManager.Draw();

        Main.Graphics.GraphicsDevice.SetRenderTarget(CameraTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);

        //draw render targets to camera target 
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Main.SpriteBatch.Draw(_mapTexture, CameraPosition * CameraScale, null, Color.White, 0, Vector2.Zero, CameraScale, SpriteEffects.None, 0f);
        Main.SpriteBatch.Draw(WaveManager.SpawnerTarget, CameraPosition * CameraScale, null, Color.White, 0, Vector2.Zero, CameraScale, SpriteEffects.None, 0f);
        Main.SpriteBatch.Draw(TowerManager.TowerTarget, CameraPosition * CameraScale, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
        Main.SpriteBatch.Draw(WaveManager.AttackerTarget, CameraPosition * CameraScale, null, Color.White, 0, Vector2.Zero, CameraScale, SpriteEffects.None, 0f);
        Main.SpriteBatch.End();
    }
}