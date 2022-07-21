using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

internal static class Camera
{
    private const int MapWidth = MapGenerator.MapWidth;
    private const int MapHeight = MapGenerator.MapHeight;
    private const int CameraWidth = 200;
    private const int CameraHeight = 200;

    private const float WidthDifference = MapWidth - CameraWidth;
    private const float HeightDifference = MapHeight - CameraHeight;
    
    private const float MoveSpeed = 4f; //how fast the camera moves
    private static Vector2 _cameraPosition = new(WidthDifference / 2f, HeightDifference / 2f); //camera initially positioned in the middle of the map

    internal static void Update(KeyboardState keyState)
    {
        Vector2 direction = Vector2.Zero; //vector that represents the direction the camera will move this frame
        
        //set direction from keyboard input
        if ((keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D)) && _cameraPosition.X + MoveSpeed <= WidthDifference) direction.X += 1;
        if ((keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A)) && _cameraPosition.X - MoveSpeed >= 0) direction.X += -1;
        if ((keyState.IsKeyDown(Keys.Down) || keyState.IsKeyDown(Keys.S)) && _cameraPosition.Y + MoveSpeed <= HeightDifference) direction.Y += 1;
        if ((keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W)) && _cameraPosition.Y - MoveSpeed >= 0) direction.Y += -1;
        
        //if a direction has been set
        if (direction != Vector2.Zero)
        {
            direction.Normalize(); //make the modulus of the vector 1 so diagonal movement is the same speed as horizontal/vertical movement
            _cameraPosition += direction * MoveSpeed; //multiply movement speed by the direction vector and add it to the camera's position
        }
    }

    internal static void DrawMap(SpriteBatch spriteBatch, Texture2D pixel, float[,] heightMap)
    {
        for (int y = 0; y < CameraHeight; y++)
        {
            for (int x = 0; x < CameraWidth; x++)
            {
                spriteBatch.Draw(pixel, new Vector2(x, y), Color.Black * heightMap[x + (int)_cameraPosition.X, y + (int)_cameraPosition.Y]);
            }
        }
    }
}