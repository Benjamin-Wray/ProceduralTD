using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class TowerManager
{
    internal enum MenuOptions
    {
        LandMine,
        Cannon,
        NailGun,
        Sniper,
        Upgrade,
        Sell
    }
    
    //The towers are drawn to this render target
    internal static readonly RenderTarget2D TowerTarget = new(Main.Graphics.GraphicsDevice, (int)(MapGenerator.MapWidth * Camera.CameraScale), (int)(MapGenerator.MapHeight * Camera.CameraScale));

    //this array is used by the program when placing towers to prevent towers from being placed on water or other towers
    internal static bool[,] InvalidPositions = new bool[MapGenerator.MapWidth, MapGenerator.MapHeight];
    
    internal static List<Tower> PlacedTowers = new(); //stores all of the placed towers
    internal static Castle Castle;
    internal static Tower? SelectedTower; //stored the tower the player will place
    private static int _hoveredTowerIndex; //index of the tower in the tower list that the mouse is currently hovering over

    private static Point _mousePositionOnMap; //position of the mouse on the map
    private static bool _isLeftMouseDown; //tells the program if the mouse is currently down

    //tower textures
    internal static Texture2D CastleBase;
    internal static Texture2D LandMineBase;
    internal static Texture2D CannonBase;
    internal static Texture2D CannonTop;
    internal static Texture2D NailGunBase;
    internal static Texture2D SniperBase;
    internal static Texture2D SniperTop;

    internal static void Initialize()
    {
        PlacedTowers.Clear();
        Castle = null!;
        
        Player.Money = Player.StartingMoney;
        Player.Health = Player.StartingHealth;
        
        SelectedTower = new Castle(); //sets the current tower to the castle
    }
    
    internal static void LoadContent()
    {
        CastleBase = Main.ContentManager.Load<Texture2D>("images/map/castle");
        
        LandMineBase = Main.ContentManager.Load<Texture2D>("images/ui/towers/landmine");
        
        CannonBase = Main.ContentManager.Load<Texture2D>("images/ui/towers/cannon_base");
        CannonTop = Main.ContentManager.Load<Texture2D>("images/ui/towers/cannon_top");

        NailGunBase = Main.ContentManager.Load<Texture2D>("images/ui/towers/nailgun");

        SniperBase = Main.ContentManager.Load<Texture2D>("images/ui/towers/sniper_base");
        SniperTop = Main.ContentManager.Load<Texture2D>("images/ui/towers/sniper_top");
    }
    
    internal static void Update(GameTime gameTime)
    {
        //get the position of the mouse on the map
        MouseState mouseState = Mouse.GetState();
        _mousePositionOnMap = WindowManager.GetMouseInRectangle(Ui.UiTarget.Bounds) - Camera.CameraPosition.ToPoint();

        if (SelectedTower != null) SelectedTower.Position = _mousePositionOnMap; //if a tower is selected, set its position to under the mouse
        else if (Ui.SelectedOption is (int)MenuOptions.Sell or (int)MenuOptions.Upgrade) GetTowerUnderMouse(); //if sell or upgrade, find the index of tower underneath the mouse

        //check if mouse was pressed this frame
        switch (mouseState.LeftButton, _isLeftMouseDown)
        {
            case (ButtonState.Pressed, false):
                _isLeftMouseDown = true;
                OnClick();
                break;
            case (ButtonState.Released, true):
                _isLeftMouseDown = false;
                break;
        }

        Tower[] tempPlacedTowers = PlacedTowers.ToArray();
        foreach (Tower tower in tempPlacedTowers)
        {
            tower.Update(gameTime);
        }
    }
    
    private static void OnClick()
    {
        if (!Camera.CameraTarget.Bounds.Contains(WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds))) return; //if the mouse is not on the map when clicked, do nothing

        if (SelectedTower != null) SelectedTower.PlaceTower(ref PlacedTowers); //if a tower is selected, place the tower
        else if (_hoveredTowerIndex >= 0) //if the mouse is over a tower
        {
            switch (Ui.SelectedOption)
            {
                case (int)MenuOptions.Upgrade: //if upgrade is selected
                    PlacedTowers[_hoveredTowerIndex].Upgrade(); //upgrade the tower
                    break;
                case (int)MenuOptions.Sell: //if sell is selected
                    PlacedTowers[_hoveredTowerIndex].Sell(ref PlacedTowers); //sell the tower
                    break;
            }
        }
    }
    
    internal static void SelectTower() //called when the selected value in Ui is changed and when a tower is placed
    {
        //sets the selected tower an instance of the corresponding class
        SelectedTower = Ui.SelectedOption switch
        {
            (int)MenuOptions.LandMine => new LandMine(),
            (int)MenuOptions.Cannon => new Cannon(),
            (int)MenuOptions.NailGun => new NailGun(),
            (int)MenuOptions.Sniper => new Sniper(),
            _ => null
        };
    }

    private static void GetTowerUnderMouse()
    {
        _hoveredTowerIndex = FindTowerIndex(); //finds the index of the tower under the mouse
        
        if (_hoveredTowerIndex >= 0) PlacedTowers[_hoveredTowerIndex].SetCursorPrice(); //if the mouse is over a tower, display its sell/upgrade price
        else Ui.CursorPrice = null; //if the mouse is not over a tower no price will be displayed
    }

    private static int FindTowerIndex() => PlacedTowers.FindIndex(x => Vector2.Distance(_mousePositionOnMap.ToVector2(), x.Position.ToVector2()) <= x.BaseTexture.Width / 2f);

    internal static void Draw()
    {
        //start drawing to the tower target
        Main.Graphics.GraphicsDevice.SetRenderTarget(TowerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        
        //begin drawing with sort mode set to Back To Front so the top parts of the tower sprites are always drawn above the base parts
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.BackToFront);

        Castle?.DrawToMap();
        foreach (Tower tower in PlacedTowers) tower.DrawToMap(); //draw the towers to the map
        SelectedTower?.DrawUnderMouse(); //draw the selected tower

        Main.SpriteBatch.End();
    }
}