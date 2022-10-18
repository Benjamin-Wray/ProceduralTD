using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                _position = new Point(Math.Clamp(_position.X, 0, MapGenerator.MapWidth - 1), Math.Clamp(_position.Y, 0, MapGenerator.MapHeight - 1));
                _topLeftPosition = Ui.CentrePosition(_position.ToVector2(), BaseTexture).ToPoint();
                CheckTowerCanBePlaced(); //check if the tower can be placed in the new position
                UpdateRange(); //update the range of the tower
            }
        }
    }

    private Texture2D _rangeIndicator;
    private readonly Color _rangeIndicatorColour = new(76, 95, 51, 100);

    private Point _topLeftPosition;
    private float _topAngle;

    private bool _isActive; //makes sure the tower only attacks once it has been placed
    internal static bool CanBePlaced;

    private const float UpgradePriceMultiplier = 3/5f; //used to calculate the upgrade price
    internal int BuyPrice; //price of the tower
    private int _upgradePrice; //Price to upgrade the tower
    internal int SellPrice; //Price to sell the tower

    protected float FireRate;
    protected float MinRange;
    private float _range;
    protected int Damage;
    
    protected Tower() => Constructor(); //The base constructor must be executed after the constructor in the subclass so it calls another function which can be overriden

    protected virtual void Constructor()
    {
        SellPrice = BuyPrice / 2;
        _upgradePrice = (int)(BuyPrice * UpgradePriceMultiplier);
    }

    internal void UpdateSpaceValidity(bool newValue)
    {
        //iterates through the square of the grid the tower is in
        Parallel.For(0, BaseTexture.Height, y =>
        {
            Parallel.For(0, BaseTexture.Width, x =>
            {
                Point setPosition = _topLeftPosition + new Point(x, y);
                //ignores anything outside of the radius of the base unless it is the castle
                if (IsInRange(setPosition, Position, BaseTexture.Width / 2f) && this is not Castle)
                {
                    //sets the position in the invalid positions list to true if the tower is being placed or false if it is being sold
                    TowerPlacement.InvalidPositions[setPosition.X, setPosition.Y] = newValue;
                }
            });
        });
    }

    private void CheckTowerCanBePlaced()
    {
        CanBePlaced = true;
        Rectangle mapBounds = new Rectangle(0, 0, MapGenerator.MapWidth, MapGenerator.MapHeight);
        if (Player.Money < BuyPrice || !mapBounds.Contains(new Rectangle(_topLeftPosition, BaseTexture.Bounds.Size)))
        {
            CanBePlaced = false; //if the player cannot afford the tower, it cannot be placed
            return; //there is no need to check the positions if the player cannot afford to place the tower
        }
        
        //iterates through the square of the grid the tower is in
        Parallel.For(0, BaseTexture.Height, y =>
        {
            Parallel.For(0, BaseTexture.Width, x =>
            {
                Point checkPosition = _topLeftPosition + new Point(x, y);
                //ignores anything outside of the radius of the base unless it is the castle
                if (IsInRange(checkPosition, Position, BaseTexture.Width / 2f) && this is not Castle)
                {
                    if (TowerPlacement.InvalidPositions[checkPosition.X, checkPosition.Y])
                    {
                        CanBePlaced = false; //if tower is on water or another tower, it cannot be placed
                    }
                }
            });
        });
    }

    protected virtual void UpdateRange()
    {
        
        _range = MinRange + MinRange * MapGenerator.NoiseMap[Position.X, Position.Y] * 2; //towers that are higher up will have a better range
        
        _rangeIndicator = new Texture2D(Main.Graphics.GraphicsDevice, (int)_range * 2 + 2, (int)_range * 2 + 2); //texture to show the range of the tower
        Color[] colours = new Color[_rangeIndicator.Width * _rangeIndicator.Height]; //create colour map for the range indicator
        
        //checks each pixel of the texture
        Parallel.For(0, _rangeIndicator.Height, y =>
        {
            Parallel.For(0, _rangeIndicator.Width, x =>
            {
                //if the pixel is in the range of the texture, it will be visible 
                if (Vector2.Distance(new Vector2(x, y), _rangeIndicator.Bounds.Center.ToVector2()) <= _range) colours[x % _rangeIndicator.Width + y * _rangeIndicator.Width] = Color.White;
            });
        });
        
        _rangeIndicator.SetData(colours); //set the colours to the texture
        
    }
    
    private static bool IsInRange(Point position, Point origin, float radius) => Vector2.Distance(position.ToVector2(), origin.ToVector2()) <= radius; //calculates the distance between two points and checks if they are within the radius
    
    internal void DrawUnderMouse()
    {
        if (!Camera.CameraTarget.Bounds.Contains(WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds))) return; //only draw the tower if the mouse is on the map
        
        Color tint = CanBePlaced ? Ui.CanBuyColour : Ui.CannotBuyColour; //set the tint of the sprite to show if the tower can be placed
        tint.A = (byte)(tint.A * .8f); //make the tower slightly transparent
        if (_rangeIndicator != null) Main.SpriteBatch.Draw(_rangeIndicator, Position.ToVector2() * Camera.CameraScale, null, _rangeIndicatorColour, 0f, _rangeIndicator.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, 1f);
        DrawToMap(tint);
    }

    internal void DrawToMap(Color drawColour = default)
    {
        if (drawColour == default) drawColour = Color.White;
        
        //draw textures to map, top textures should always be drawn above base textures
        Main.SpriteBatch.Draw(BaseTexture, Position.ToVector2() * Camera.CameraScale, null, drawColour, 0f, BaseTexture.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, 1f); //draw base texture
        if (TopTexture != null) Main.SpriteBatch.Draw(TopTexture, Position.ToVector2() * Camera.CameraScale, null, drawColour, _topAngle, TopTexture.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, 0f); //draw top texture
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
        Ui.CursorPrice = Ui.SelectedOption switch
        {
            (int) TowerPlacement.MenuOptions.Sell => SellPrice,
            (int) TowerPlacement.MenuOptions.Upgrade => _upgradePrice,
            _ => null
        };
    }

    internal virtual void Upgrade()
    {
        if (Player.Money < _upgradePrice) return; //only upgrades if the player has enough money
        FireRate *= 2;
        SellPrice += _upgradePrice / 2;
        Player.Money -= _upgradePrice;
        _upgradePrice *= 2;
    }

    internal void Sell(ref List<Tower> towers)
    {
        Player.Money += SellPrice;
        UpdateSpaceValidity(false); //removes tower from invalid positions so other towers can be placed where it was
        towers.Remove(this); //remove tower from map
    }
    
    internal void Update()
    {
        _topAngle += .1f;
        _topAngle %= 360;
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

    protected override void UpdateRange()
    {
        //ignore
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
        MinRange = 10;
        Damage = 5;
        
        base.Constructor();
    }

    internal override void DisplayCursorPrice()
    {
        //only display sell price
        if (Ui.SelectedOption == (int)TowerPlacement.MenuOptions.Sell) Ui.CursorPrice = SellPrice;
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
        MinRange = 30;
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
        MinRange = 25;
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
        MinRange = 40;
        Damage = 6;

        base.Constructor();
    }
}