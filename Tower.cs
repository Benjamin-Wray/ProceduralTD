using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
                if (this is Castle) _topLeftPosition.Y -= BaseTexture.Height / 2;
                CheckTowerCanBePlaced(); //check if the tower can be placed in the new position
                UpdateRange(); //update the range of the tower
            }
        }
    }

    private Texture2D _rangeIndicator;
    private readonly Color _rangeIndicatorColour = new(76, 95, 51, 100);

    private Point _topLeftPosition;
    private float _topAngle;

    internal static bool CanBePlaced;

    private const float UpgradePriceMultiplier = 3/5f; //used to calculate the upgrade price
    internal int BuyPrice; //price of the tower
    private int _upgradePrice; //Price to upgrade the tower
    internal int SellPrice; //Price to sell the tower

    private float _fireTimer;
    protected float TimeToFire;
    protected float MinRange;
    protected float Range;
    protected int Damage;
    
    protected Tower() => Constructor(); //The base constructor must be executed after the constructor in the subclass so it calls another function which can be overriden

    protected virtual void Constructor()
    {
        SellPrice = BuyPrice / 2;
        _upgradePrice = (int)(BuyPrice * UpgradePriceMultiplier);
    }

    protected void UpdateTowerSpaceValidity(bool newValue) => UpdateSpaceValidity(newValue, BaseTexture, Position, _topLeftPosition, this is Castle);

    internal static void UpdateSpaceValidity(bool newValue, Texture2D texture, Point position, Point topLeftPosition, bool isCastle = false)
    {
        //iterates through the square of the grid the tower is in
        for (int y = 0; y < texture.Height; y++)
        {
            for (int x = 0; x < texture.Width; x++)
            {
                Point setPosition = topLeftPosition + new Point(x, y);
                //ignores anything outside of the radius of the base unless it is the castle
                if (IsInRadius(setPosition, position, texture.Width / 2f) || isCastle)
                {
                    //sets the position in the invalid positions list to true if the tower is being placed or false if it is being sold
                    TowerPlacement.InvalidPositions[setPosition.X, setPosition.Y] = newValue;
                }
            }
        }
    }

    internal void CheckTowerCanBePlaced()
    {
        CanBePlaced = true; //initially, the tower can be placed
        if (Player.Money < BuyPrice || !MapGenerator.MapBounds.Contains(new Rectangle(_topLeftPosition, BaseTexture.Bounds.Size)))
        {
            CanBePlaced = false; //if the player cannot afford the tower, it cannot be placed
            return; //there is no need to check the positions if the player cannot afford to place the tower
        }
        
        //iterates through the square of the grid the tower is in
        for (int y = 0; y < BaseTexture.Height; y++)
        {
            for (int x = 0; x < BaseTexture.Width; x++)
            {
                Point checkPosition = _topLeftPosition + new Point(x, y);
                //ignores anything outside of the radius of the base unless it is the castle
                if (IsInRadius(checkPosition, Position, BaseTexture.Width / 2f) || this is Castle)
                {
                    if (TowerPlacement.InvalidPositions[checkPosition.X, checkPosition.Y])
                    {
                        CanBePlaced = false; //if tower is on water or another tower, it cannot be placed
                    }
                }
            }
        }
    }

    protected virtual void UpdateRange()
    {
        Range = MinRange + MinRange * MapGenerator.NoiseMap[Position.X, Position.Y] * 2; //towers that are higher up will have a better range
        
        _rangeIndicator = new Texture2D(Main.Graphics.GraphicsDevice, (int)Range * 2 + 2, (int)Range * 2 + 2); //texture to show the range of the tower
        Color[] colours = new Color[_rangeIndicator.Width * _rangeIndicator.Height]; //create colour map for the range indicator
        
        //checks each pixel of the texture
        for (int y = 0; y < _rangeIndicator.Height; y++)
        {
            for (int x = 0; x < _rangeIndicator.Width; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), _rangeIndicator.Bounds.Center.ToVector2()) <= Range) colours[x % _rangeIndicator.Width + y * _rangeIndicator.Width] = Color.White;
            }
        }
        
        _rangeIndicator.SetData(colours); //set the colours to the texture
        
    }

    internal static bool IsInRadius(Point position, Point origin, float radius) => Vector2.Distance(position.ToVector2(), origin.ToVector2()) <= radius; //calculates the distance between two points and checks if they are within the radius
    
    internal void DrawUnderMouse()
    {
        if (!Camera.CameraTarget.Bounds.Contains(WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds))) return; //only draw the tower if the mouse is on the map
        
        Color tint = new Color(CanBePlaced ? Ui.CanBuyColour : Ui.CannotBuyColour, 200); //set the tint of the sprite to show if the tower can be placed
        DrawToMap(tint);
    }

    internal virtual void DrawToMap(Color drawColour = default)
    {
        if (drawColour == default) drawColour = Color.White;
        Vector2 drawPosition = Position.ToVector2() * Camera.CameraScale;
        
        //draw textures to map, top textures should always be drawn above base textures
        if (Ui.SelectedOption != null && _rangeIndicator != null) Main.SpriteBatch.Draw(_rangeIndicator, drawPosition, null, _rangeIndicatorColour, 0f, _rangeIndicator.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, 1f);
        Main.SpriteBatch.Draw(BaseTexture, drawPosition, null, drawColour, 0f, BaseTexture.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, .5f); //draw base texture
        if (TopTexture != null) Main.SpriteBatch.Draw(TopTexture, drawPosition, null, drawColour, _topAngle, TopTexture.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, 0f); //draw top texture
    }

    internal virtual void PlaceTower(ref List<Tower> towers)
    {
        if (!CanBePlaced) return; //only place the tower if it can be placed
        towers.Add(this); //add this tower to the list of placed towers
        UpdateTowerSpaceValidity(true); //update the invalid positions array so other towers cannot be placed on top of this one
        Player.Money -= BuyPrice; //subtract the buy price from the player's money
        TowerPlacement.SelectTower(); //sets a new tower instance to be placed
    }

    internal virtual void SetCursorPrice()
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
        TimeToFire /= 2;
        SellPrice += _upgradePrice / 2;
        Player.Money -= _upgradePrice;
        _upgradePrice *= 2;
    }

    internal void Sell(ref List<Tower> towers)
    {
        Player.Money += SellPrice;
        UpdateTowerSpaceValidity(false); //removes tower from invalid positions so other towers can be placed where it was
        towers.Remove(this); //remove tower from map
    }
    
    internal void Update(GameTime gameTime)
    {
        Attacker[] attackersInRange = WaveManager.Attackers.Where(attacker => DistanceFromTower(attacker) <= Range).ToArray();
        if (attackersInRange.Length == 0)
        {
            _fireTimer = 0;
            return;
        }

        attackersInRange = OrderAttackers(attackersInRange);
        _topAngle = MathF.Atan2(attackersInRange.First().Position.Y - _position.Y, attackersInRange.First().Position.X - _position.X);

        _fireTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_fireTimer >= TimeToFire)
        {
            _fireTimer %= TimeToFire;
            Fire(attackersInRange);
        }
    }

    private float DistanceFromTower(Attacker attacker) => Vector2.Distance(attacker.Position.ToVector2(), Position.ToVector2());
    protected float DistanceFromCastle(Attacker attacker) => Vector2.Distance(attacker.Position.ToVector2(), Player.Castle.Position.ToVector2());
    
    protected virtual Attacker[] OrderAttackers(Attacker[] attackers) => attackers;

    protected virtual void Fire(Attacker[] attackers)
    {
        Attacker attackerToShoot = attackers.First();
        attackerToShoot.Hp -= Damage;
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
        UpdateTowerSpaceValidity(true);
        TowerPlacement.SelectedTower = null; //stop placing castle
        StateMachine.ChangeState(StateMachine.Action.PlaceCastle); //update the machine state to allow the player to place other towers
    }

    protected override void UpdateRange()
    {
        //ignore
    }
    
    internal override void DrawToMap(Color drawColour = default)
    {
        if (drawColour == default) drawColour = Color.White;
        Vector2 drawPosition = Position.ToVector2() * Camera.CameraScale;
        
        //draw textures to map, top textures should always be drawn above base textures
        Main.SpriteBatch.Draw(BaseTexture, drawPosition, null, drawColour, 0f, new Vector2(BaseTexture.Bounds.Center.X, BaseTexture.Bounds.Bottom), Camera.CameraScale, SpriteEffects.None, .5f); //draw base texture
    }
}

internal class LandMine : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerPlacement.LandMineBase;
        BuyPrice = 10;
        TimeToFire = 2;
        MinRange = 15;
        Damage = 5;
        
        base.Constructor();
    }

    internal override void SetCursorPrice()
    {
        //only display sell price
        if (Ui.SelectedOption == (int)TowerPlacement.MenuOptions.Sell) Ui.CursorPrice = SellPrice;
        else Ui.CursorPrice = null;
    }

    internal override void Upgrade()
    {
        //Landmines cannot be upgraded
    }

    protected override void Fire(Attacker[] attackers)
    {
        foreach (Attacker attacker in attackers)
        {
            attacker.Hp -= Damage;
        }
        UpdateTowerSpaceValidity(false);
        TowerPlacement.PlacedTowers.Remove(this);
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
        TimeToFire = 3;
        MinRange = 30;
        Damage = 3;
        
        base.Constructor();
    }

    protected override Attacker[] OrderAttackers(Attacker[] attackers) => attackers.OrderBy(DistanceFromCastle).ToArray();
}

internal class NailGun : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerPlacement.NailGunBase;
        BuyPrice = 75;
        TimeToFire = 2.5f;
        MinRange = 25;
        Damage = 1;

        base.Constructor();
    }

    protected override void Fire(Attacker[] attackers)
    {
        for (int i = 0; i < Math.Min(attackers.Length, 4); i++) attackers[i].Hp -= Damage;
    }
}

internal class Sniper : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerPlacement.SniperBase;
        TopTexture = TowerPlacement.SniperTop;
        BuyPrice = 45;
        TimeToFire = 6;
        MinRange = 40;
        Damage = Attacker.MaxHp;

        base.Constructor();
    }

    protected override Attacker[] OrderAttackers(Attacker[] attackers) => attackers.OrderByDescending(x => x.Hp).ThenBy(DistanceFromCastle).ToArray();
}