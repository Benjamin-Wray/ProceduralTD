using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal abstract class Tower
{
    //textures
    internal Texture2D BaseTexture; //every tower has a base texture
    internal Texture2D TopTexture; //top part that rotates to point to nearest enemy

    //position of the tower
    private Point _position;
    internal Point Position
    {
        get => _position;
        set
        {
            if (_position != value) //if the position has changed
            {
                _position = value; //set the new position
                CheckTowerCanBePlaced(); //check if the tower can be placed in the new position

                //update the draw positions
                BaseDrawPosition = Ui.CentrePosition(_position.ToVector2(), BaseTexture);
                if (TopTexture != null) _topDrawPosition = Ui.CentrePosition(_position.ToVector2(), TopTexture);
            }
        }
    }

    //positions the textures are drawn at
    internal Vector2 BaseDrawPosition;
    private Vector2 _topDrawPosition;

    private bool _isActive; //makes sure the tower only attacks once it has been placed
    internal static bool CanBePlaced;

    private const float UpgradePriceMultiplier = 3/5f; //used to calculate the upgrade price
    internal int BuyPrice; //price of the tower
    internal int UpgradePrice; //Price to upgrade the tower
    internal int SellPrice; //Price to sell the tower

    protected float FireRate;
    protected int Damage;
    
    protected Tower() => Constructor(); //The base constructor must be executed after the constructor in the subclass so it calls another function which can be overriden

    protected virtual void Constructor()
    {
        SellPrice = BuyPrice / 2;
        UpgradePrice = (int)(BuyPrice * UpgradePriceMultiplier);
    }

    internal void UpdateSpaceValidity(bool newValue)
    {
        //iterates through the square of the grid the tower is in
        for (int y = 0; y < BaseTexture.Height; y++)
        {
            for (int x = 0; x < BaseTexture.Width; x++)
            {
                //ignores anything outside of the radius of the base unless it is the castle
                if (!IsInRange(BaseDrawPosition.ToPoint() + new Point(x, y), Position, BaseTexture.Width / 2f) && this is not Castle) continue;
                TowerPlacement.InvalidPositions[(int)BaseDrawPosition.X + x, (int)BaseDrawPosition.Y + y] = newValue; //sets the position in the invalid positions list to true if the tower is being placed or false if it is being sold
            }
        }
    }

    private void CheckTowerCanBePlaced()
    {
        if (Player.Money >= BuyPrice) CanBePlaced = true;
        else
        {
            CanBePlaced = false; //if the player cannot afford the tower, it cannot be placed
            return; //there is no need to check the positions if the player cannot afford to place the tower
        }
        
        //iterates through the square of the grid the tower is in
        for (int y = 0; y < BaseTexture.Height; y++)
        {
            for (int x = 0; x < BaseTexture.Width; x++)
            {
                //ignores anything outside of the radius of the base unless it is the castle
                if (!IsInRange(BaseDrawPosition.ToPoint() + new Point(x, y), Position, BaseTexture.Width / 2f) && this is not Castle) continue;
                if (Camera.CameraTarget.Bounds.Contains(BaseDrawPosition.ToPoint() + new Point(x, y))) //if tower is on map
                {
                    if (TowerPlacement.InvalidPositions[x + (int) BaseDrawPosition.X, y + (int) BaseDrawPosition.Y])
                    {
                        CanBePlaced = false; //if tower is on water or another tower, it cannot be placed
                        return; //there is no need to check the other positions because we know the tower can't be placed
                    }
                }
                else
                {
                    CanBePlaced = false; //if the tower is on the edge of the map, it cannot be placed
                    return; //there is no need to check the other positions if the tower is not fully in on map
                }
            }
        }
    }
    
    private static bool IsInRange(Point position, Point origin, float radius) => Vector2.Distance(position.ToVector2(), origin.ToVector2()) <= radius; //calculates the distance between two points and checks if they are within the radius
    
    internal void DrawUnderMouse()
    {
        if (!Camera.CameraTarget.Bounds.Contains(WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds))) return; //only draw the tower if the mouse is on the map
        
        Color tint = CanBePlaced ? Ui.CanBuyColour : Ui.CannotBuyColour; //set the tint of the sprite to show if the tower can be placed
        tint.A = (byte)(tint.A * .8f); //make the tower slightly transparent
        Main.SpriteBatch.Draw(BaseTexture, Ui.CentrePosition(Position.ToVector2(), BaseTexture), tint); //draw the base texture below the mouse
        if (TopTexture != null) Main.SpriteBatch.Draw(TopTexture, Ui.CentrePosition(Position.ToVector2(), TopTexture), tint); //draw the top texture if there is one to draw
    }

    internal void DrawToMap()
    {
        //draw textures to map, top textures should always be drawn above base textures
        Main.SpriteBatch.Draw(BaseTexture, BaseDrawPosition, null, Color.White, 0f, Vector2.Zero, 1, SpriteEffects.None, 1f); //draw base texture
        if (TopTexture != null) Main.SpriteBatch.Draw(TopTexture, _topDrawPosition, null, Color.White, 0f, Vector2.Zero, 1, SpriteEffects.None, 0f); //draw top texture
    }

    internal virtual void PlaceTower(ref List<Tower> towers)
    {
        if (!CanBePlaced) return; //only place the tower if it can be placed
        towers.Add(this); //add this tower to the list of placed towers
        UpdateSpaceValidity(true); //update the invalid positions array so other towers cannot be placed on top of this one
        _isActive = true; //set the tower to active
        Player.Money -= BuyPrice; //subtract the buy price from the player's money
        TowerPlacement.SelectTower(); //sets a new tower instance to be placed
    }

    internal virtual void DisplayCursorPrice()
    {
        //display the corresponding price
        Ui.CursorPrice = Ui.Selected switch
        {
            (int) TowerPlacement.Towers.Sell => SellPrice,
            (int) TowerPlacement.Towers.Upgrade => UpgradePrice,
            _ => null
        };
    }

    internal virtual void Upgrade()
    {
        if (Player.Money < UpgradePrice) return; //only upgrades if the player has enough money
        FireRate *= 2;
        SellPrice += UpgradePrice / 2;
        Player.Money -= UpgradePrice;
        UpgradePrice *= 2;
    }

    internal void Sell(ref List<Tower> towers)
    {
        Player.Money += SellPrice;
        UpdateSpaceValidity(false); //removes tower from invalid positions so other towers can be placed where it was
        towers.Remove(this); //remove tower from map
    }
}

internal class Castle : Tower
{
    protected override void Constructor()
    {
        BaseTexture = TowerPlacement.CastleBase;
        base.Constructor();
    }

    internal override void PlaceTower(ref List<Tower> towers)
    {
        if (!CanBePlaced) return; //only place the castle if it is not on water
        Player.Castle = this; //place castle
        UpdateSpaceValidity(true);
        TowerPlacement.SelectedTower = null; //stop placing castle
        StateMachine.ChangeState(StateMachine.Action.PlaceCastle); //update the machine state to allow the player to place other towers
    }
}

internal class LandMine : Tower
{
    protected override void Constructor( )
    {
        //set values
        BaseTexture = TowerPlacement.LandMineBase;
        BuyPrice = 10;
        FireRate = 1;
        Damage = 5;
        
        base.Constructor();
    }

    internal override void DisplayCursorPrice()
    {
        //only display sell price
        if (Ui.Selected == (int)TowerPlacement.Towers.Sell) Ui.CursorPrice = SellPrice;
        else Ui.CursorPrice = null;
    }

    internal override void Upgrade()
    {
        //Landmines cannot be upgraded
    }
}

internal class Cannon : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerPlacement.CannonBase;
        TopTexture = TowerPlacement.CannonTop;
        BuyPrice = 30;
        FireRate = .5f;
        Damage = 3;
        
        base.Constructor();
    }
}

internal class NailGun : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerPlacement.NailGunBase;
        BuyPrice = 45;
        FireRate = 1;
        Damage = 2;
        
        base.Constructor();
    }
}

internal class Sniper : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerPlacement.SniperBase;
        TopTexture = TowerPlacement.SniperTop;
        BuyPrice = 75;
        FireRate = .2f;
        Damage = 6;

        base.Constructor();
    }
}