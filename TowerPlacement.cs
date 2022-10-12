using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ProceduralTD;

public static class TowerPlacement
{
    internal enum Towers
    {
        LandMine,
        Cannon,
        NailGun,
        Sniper,
        Upgrade,
        Sell
    }

    //The towers are drawn to this render target
    internal static readonly RenderTarget2D TowerTarget = new(Main.Graphics.GraphicsDevice, MapGenerator.MapWidth, MapGenerator.MapHeight);

    //this array is used by the program when placing towers to prevent towers from being placed on water or other towers
    internal static readonly bool[,] InvalidPositions = new bool[MapGenerator.MapWidth, MapGenerator.MapHeight];
    
    private static List<Tower> _towers = new(); //stores all of the placed towers
    internal static Tower? SelectedTower; //stored the tower the player will place
    private static int? _hoveredTowerIndex; //index of the tower in the tower list that the mouse is currently hovering over

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
    
    internal static void Update()
    {
        //get the position of the mouse on the map
        MouseState mouseState = Mouse.GetState();
        _mousePositionOnMap = WindowManager.GetMouseInRectangle(Ui.UiTarget.Bounds) - Camera.CameraPosition.ToPoint();
        
        if (SelectedTower != null) SelectedTower.Position = _mousePositionOnMap; //if a tower is selected, set its position to under the mouse
        else if (Ui.Selected is (int)Towers.Sell or (int)Towers.Upgrade) GetTowerUnderMouse(); //if sell or upgrade, find the index of tower underneath the mouse

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
    }

    internal static void SelectCastle() => SelectedTower = new Castle(); //runs at start of game, sets the current tower to the castle
    
    internal static void SelectTower() //called when the selected value in Ui is changed and when a tower is placed
    {
        //sets the selected tower an instance of the corresponding class
        SelectedTower = Ui.Selected switch
        {
            (int)Towers.LandMine => new LandMine(),
            (int)Towers.Cannon => new Cannon(),
            (int)Towers.NailGun => new NailGun(),
            (int)Towers.Sniper => new Sniper(),
            _ => null
        };
    }

    private static void GetTowerUnderMouse()
    {
        _hoveredTowerIndex = FindTowerIndex(); //finds the index of the tower under the mouse
        
        if (_hoveredTowerIndex.HasValue) _towers[_hoveredTowerIndex.Value].DisplayCursorPrice(); //if the mouse is over a tower, display its sell/upgrade price
        else Ui.CursorPrice = null; //if the mouse is not over a tower no price will be displayed
    }

    private static int? FindTowerIndex()
    {
        //iterates through each tower in the list
        for (int i = 0; i < _towers.Count; i++)
        {
            //checks if the distance between the mouse and the tower is less than or equal to the radius of the tower
            if (Vector2.Distance(_mousePositionOnMap.ToVector2(), _towers[i].Position.ToVector2()) <= _towers[i].BaseTexture.Width / 2f)
            {
                return i; //returns the index of this tower
            }
        }
        return null; //the mouse is not hovering over any towers so tower index is null
    }

    private static void OnClick()
    {
        if (!Camera.CameraTarget.Bounds.Contains(WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds))) return; //if the mouse is not on the map when clicked, do nothing

        if (SelectedTower != null) SelectedTower.PlaceTower(ref _towers); //if a tower is selected, place the tower
        else if (_hoveredTowerIndex != null) //if the mouse is over a tower
        {
            switch (Ui.Selected)
            {
                case (int)Towers.Upgrade: //if upgrade is selected
                    _towers[_hoveredTowerIndex.Value].Upgrade(); //upgrade the tower
                    break;
                case (int)Towers.Sell: //if sell is selected
                    _towers[_hoveredTowerIndex.Value].Sell(ref _towers); //sell the tower
                    break;
            }
        }
    }

    internal static void Draw()
    {
        //start drawing to the tower target
        Main.Graphics.GraphicsDevice.SetRenderTarget(TowerTarget);
        Main.Graphics.GraphicsDevice.Clear(Color.Transparent);
        
        //begin drawing with sort mode set to Back To Front so the top parts of the tower sprites are always drawn above the base parts
        Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.BackToFront);

        if (Player.Castle != null) Main.SpriteBatch.Draw(Player.Castle.BaseTexture, Player.Castle.BaseDrawPosition, Color.White); //draw the castle
        foreach (Tower tower in _towers) tower.DrawToMap(); //draw the towers to the map
        SelectedTower?.DrawUnderMouse(); //draw the selected tower

        Main.SpriteBatch.End();
    }
}