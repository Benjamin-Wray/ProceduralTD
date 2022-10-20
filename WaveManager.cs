using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class WaveManager
{
    internal static int CurrentWave;
    private const int AddSpawnerInterval = 5;
    
    internal static readonly RenderTarget2D AttackerTarget = new(Main.Graphics.GraphicsDevice, MapGenerator.MapWidth, MapGenerator.MapHeight);

    internal static List<Spawner> Spawners = new();
    private static List<Attacker> _attackers = new();

    internal static Texture2D Pixel;
    internal static Texture2D[] AttackerColours;
    internal static Texture2D[] PortalFrames;
    
    internal static void LoadContent()
    {
        Pixel = new Texture2D(Main.Graphics.GraphicsDevice, 1, 1);
        Pixel.SetData(new[] {Color.White});
        
        AttackerColours = new[]
        {
            Main.ContentManager.Load<Texture2D>("images/map/bubbles/bubble_blue"),
            Main.ContentManager.Load<Texture2D>("images/map/bubbles/bubble_green"),
            Main.ContentManager.Load<Texture2D>("images/map/bubbles/bubble_yellow"),
            Main.ContentManager.Load<Texture2D>("images/map/bubbles/bubble_orange"),
            Main.ContentManager.Load<Texture2D>("images/map/bubbles/bubble_red"),
            Main.ContentManager.Load<Texture2D>("images/map/bubbles/bubble_purple")
        };
        
        PortalFrames = new[]
        {
            Main.ContentManager.Load<Texture2D>("images/map/portal/portal1"),
            Main.ContentManager.Load<Texture2D>("images/map/portal/portal2"),
            Main.ContentManager.Load<Texture2D>("images/map/portal/portal3"),
            Main.ContentManager.Load<Texture2D>("images/map/portal/portal4")
        };
    }

    internal static void UpdateWave()
    {
        if (_attackers.Count == 0) CurrentWave++;
        else return;

        if (Spawners.Count == 0 || CurrentWave % AddSpawnerInterval == 0)
        {
            Spawners.Add(new Spawner());
        }
    }

    public static void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Enter)) Spawners.Add(new Spawner());
        
        Parallel.ForEach(Spawners, spawner => spawner.Update(gameTime));
        Parallel.ForEach(_attackers, attacker => attacker.Update(gameTime));
    }

    public static void Draw()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(AttackerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        foreach (Spawner spawner in Spawners) spawner.Draw();
        foreach (Attacker attacker in _attackers) attacker.Draw();
        
        Main.SpriteBatch.End();
    }
}