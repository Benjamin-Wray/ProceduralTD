using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

public static class WaveManager
{
    internal static int CurrentWave;
    private const int AddSpawnerInterval = 5;

    internal static readonly RenderTarget2D SpawnerTarget = new(Main.Graphics.GraphicsDevice, MapGenerator.MapWidth, MapGenerator.MapHeight);
    internal static readonly RenderTarget2D AttackerTarget = new(Main.Graphics.GraphicsDevice, MapGenerator.MapWidth, MapGenerator.MapHeight);

    internal static readonly List<Spawner> Spawners = new();
    internal static readonly List<Attacker> Attackers = new();

    internal static Texture2D Pixel;
    internal static Texture2D[] AttackerColours;
    internal static Texture2D[] PortalFrames;

    internal static void Initialize()
    {
        CurrentWave = 0;
        Spawners.Clear();
        Attackers.Clear();
    }
    
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
        if (Attackers.Count == 0) CurrentWave++;
        else return;

        if (Spawners.Count == 0 || CurrentWave % AddSpawnerInterval == 0) Spawners.Add(new Spawner());
    }

    public static void Update(GameTime gameTime)
    {
        foreach (Spawner spawner in Spawners.ToArray()) spawner.Update(gameTime);
        foreach (Attacker attacker in Attackers.ToArray()) attacker.Update(gameTime);

        if (Spawners.All(x => !x.CanSpawn) && Attackers.Count == 0)
        {
            if (CurrentWave % Attacker.MaxHp == 0) Spawners.Add(new Spawner());
            CurrentWave++;
            foreach (Spawner spawner in Spawners.ToArray()) spawner.UpdateAttackersToSpawn();
        }
    }

    public static void Draw()
    {
        Main.Graphics.GraphicsDevice.SetRenderTarget(SpawnerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        foreach (Spawner spawner in Spawners) spawner.Draw();
        Main.SpriteBatch.End();
        
        Main.Graphics.GraphicsDevice.SetRenderTarget(AttackerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        foreach (Attacker attacker in Attackers) attacker.Draw();
        Main.SpriteBatch.End();
    }
}