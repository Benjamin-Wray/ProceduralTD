using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

public static class WaveManager
{
    internal static int CurrentWave;

    internal static readonly RenderTarget2D SpawnerTarget = new(Main.Graphics.GraphicsDevice, MapGenerator.MapWidth, MapGenerator.MapHeight);
    internal static readonly RenderTarget2D AttackerTarget = new(Main.Graphics.GraphicsDevice, MapGenerator.MapWidth, MapGenerator.MapHeight);

    internal static readonly List<Spawner> Spawners = new();
    internal static readonly List<Attacker> Attackers = new();

    internal static Texture2D Pixel;
    internal static Texture2D[] AttackerColours;
    internal static Texture2D?[] PortalFrames;

    internal static void Initialize()
    {
        CurrentWave = 0; //reset wave to 0
        
        //remove spawners and attackers from the map
        Spawners.Clear();
        Attackers.Clear();
    }
    
    internal static void LoadContent()
    {
        //create pixel texture
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

    internal static void StartWaves()
    {
        //set current wave to 1 and create first spawner
        CurrentWave++;
        Spawners.Add(new Spawner());
    }

    public static void Update(GameTime gameTime)
    {
        if (Spawners.All(x => !x.CanSpawn) && Attackers.Count == 0) //if the spawners have finished spawning and there are no more attackers, the wave has ended
        {
            if (CurrentWave % Attacker.MaxHp == 0) Spawners.Add(new Spawner()); //add a new spawner every 6 waves
            CurrentWave++;
            foreach (Spawner spawner in Spawners) spawner.UpdateAttackersToSpawn(); //update spawner's attackersToSpawn list
        }

        //update spawners and attackers
        foreach (Spawner spawner in Spawners.ToArray()) spawner.Update(gameTime);
        foreach (Attacker attacker in Attackers.ToArray()) attacker.Update(gameTime);
    }

    public static void Draw()
    {
        //draw spawners
        Main.Graphics.GraphicsDevice.SetRenderTarget(SpawnerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        foreach (Spawner spawner in Spawners) spawner.Draw();
        Main.SpriteBatch.End();
        
        //draw attackers
        Main.Graphics.GraphicsDevice.SetRenderTarget(AttackerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        foreach (Attacker attacker in Attackers) attacker.Draw();
        Main.SpriteBatch.End();
    }
}