using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTD;

internal abstract class Tower
{
    //textures
    internal Texture2D? BaseTexture; //every tower has a base texture
    internal Texture2D? TopTexture; //top part that rotates to point to nearest enemy

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

    private const float UpgradePriceMultiplier = 1f; //used to calculate the upgrade price
    private const float SellPriceMultiplier = 0.5f; //used to calculate the sell price
    internal int BuyPrice; //price of the tower
    private int _upgradePrice; //Price to upgrade the tower
    internal int SellPrice; //Price to sell the tower

    private float _fireTimer;
    protected float TimeToFire;
    protected float MinRange;
    private float _range;
    protected int Damage;
    
    protected Tower() => Constructor(); //The base constructor must be executed after the constructor in the subclass so it calls another function which can be overriden

    protected virtual void Constructor()
    {
        SellPrice = (int)(BuyPrice * SellPriceMultiplier);
        _upgradePrice = (int)(BuyPrice * UpgradePriceMultiplier);
    }

    protected void UpdateTowerSpaceValidity(bool newValue) => UpdateSpaceValidity(newValue, BaseTexture, Position, _topLeftPosition, this is Castle);

    internal static void UpdateSpaceValidity(bool newValue, Texture2D? texture, Point position, Point topLeftPosition, bool isCastle = false)
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
                    TowerManager.InvalidPositions[setPosition.X, setPosition.Y] = newValue;
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
                    if (TowerManager.InvalidPositions[checkPosition.X, checkPosition.Y])
                    {
                        CanBePlaced = false; //if tower is on water or another tower, it cannot be placed
                    }
                }
            }
        }
    }

    protected virtual void UpdateRange()
    {
        _range = MinRange + MinRange * MapGenerator.NoiseMap[Position.X, Position.Y] * 2; //towers that are higher up will have a better range
        
        _rangeIndicator = new Texture2D(Main.Graphics.GraphicsDevice, (int)_range * 2 + 2, (int)_range * 2 + 2); //texture to show the range of the tower
        Color[] colours = new Color[_rangeIndicator.Width * _rangeIndicator.Height]; //create colour map for the range indicator
        
        //checks each pixel of the texture
        for (int y = 0; y < _rangeIndicator.Height; y++)
        {
            for (int x = 0; x < _rangeIndicator.Width; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), _rangeIndicator.Bounds.Center.ToVector2()) <= _range) colours[x % _rangeIndicator.Width + y * _rangeIndicator.Width] = Color.White;
            }
        }
        
        _rangeIndicator.SetData(colours); //set the colours to the texture
        
    }

    internal static bool IsInRadius(Point position, Point origin, float radius) => Vector2.Distance(position.ToVector2(), origin.ToVector2()) <= radius; //calculates the distance between two points and checks if they are within the radius
    
    internal void DrawUnderMouse()
    {
        if (!Camera.CameraTarget.Bounds.Contains(WindowManager.GetMouseInRectangle(WindowManager.Scene.Bounds))) return; //only draw the tower if the mouse is on the map
        
        Color tint = new Color(CanBePlaced ? Ui.CanBuyColour : Ui.CannotBuyColour, 200); //set the tint of the sprite to show if the tower can be placed
        DrawToMap(tint, true);
    }

    internal virtual void DrawToMap(Color drawColour = default, bool selected = false)
    {
        if (drawColour == default) drawColour = Color.White;
        float layerOffset = selected ? 0f : .5f;
        Vector2 drawPosition = Position.ToVector2() * Camera.CameraScale;

        //draw textures to map, top textures should always be drawn above base textures
        if (Ui.SelectedOption != null && _rangeIndicator != null) Main.SpriteBatch.Draw(_rangeIndicator, drawPosition, null, _rangeIndicatorColour, 0f, _rangeIndicator.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, 1f);
        Main.SpriteBatch.Draw(BaseTexture, drawPosition, null, drawColour, 0f, BaseTexture.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, .1f + layerOffset); //draw base texture
        if (TopTexture != null) Main.SpriteBatch.Draw(TopTexture, drawPosition, null, drawColour, _topAngle, TopTexture.Bounds.Center.ToVector2(), Camera.CameraScale, SpriteEffects.None, 0f + layerOffset); //draw top texture
    }

    internal virtual void PlaceTower(ref List<Tower> towers)
    {
        if (!CanBePlaced) return; //only place the tower if it can be placed
        towers.Add(this); //add this tower to the list of placed towers
        UpdateTowerSpaceValidity(true); //update the invalid positions array so other towers cannot be placed on top of this one
        Player.Money -= BuyPrice; //subtract the buy price from the player's money
        TowerManager.SelectTower(); //sets a new tower instance to be placed
    }

    internal virtual void SetCursorPrice()
    {
        //display the corresponding price
        Ui.CursorPrice = Ui.SelectedOption switch
        {
            (int) TowerManager.MenuOptions.Sell => SellPrice,
            (int) TowerManager.MenuOptions.Upgrade => _upgradePrice,
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
        Attacker[] attackersInRange = WaveManager.Attackers.Where(attacker => DistanceFromTower(attacker) <= _range).ToArray(); //get attackers positioned within the tower's range
        if (attackersInRange.Length == 0) //if there are no attackers in range
        {
            _fireTimer = 0;
            return;
        }

        attackersInRange = OrderAttackers(attackersInRange); //sort attackers differently depending on the tower
        _topAngle = MathF.Atan2(attackersInRange.First().Position.Y - _position.Y, attackersInRange.First().Position.X - _position.X); //calculate angle from tower to attacker

        _fireTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_fireTimer >= TimeToFire)
        {
            _fireTimer %= TimeToFire;
            Fire(attackersInRange);
        }
    }
    
    private float DistanceFromTower(Attacker attacker) => Vector2.Distance(attacker.Position.ToVector2(), Position.ToVector2()); //returns attacker's distance from the tower
    
    protected virtual Attacker[] OrderAttackers(Attacker[] attackers) => attackers; //leave attackers in original order by default

    protected virtual void Fire(Attacker[] attackers)
    {
        //select first attacker in the list and subtract the tower's damage from it's health
        Attacker attackerToShoot = attackers.First();
        Player.Money += Math.Min(attackerToShoot.Hp, Damage);
        attackerToShoot.Hp -= Damage;
    }
}

internal class Castle : Tower
{
    protected override void Constructor()
    {
        //castle only has a texture
        BaseTexture = TowerManager.CastleBase;
        base.Constructor();
    }

    internal override void PlaceTower(ref List<Tower> towers)
    {
        if (!CanBePlaced) return; //only place the castle if it is not on water
        TowerManager.Castle = this; //place castle
        UpdateTowerSpaceValidity(true);
        TowerManager.SelectedTower = null; //stop placing castle
        StateMachine.ChangeState(StateMachine.Action.PlaceCastle); //update the machine state to allow the player to place other towers
    }

    protected override void UpdateRange()
    {
        //castles do not have range so we ignore this subroutine
    }
    
    internal override void DrawToMap(Color drawColour = default, bool selected = false)
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
        BaseTexture = TowerManager.LandMineBase;
        BuyPrice = 10;
        TimeToFire = 2;
        MinRange = 15;
        Damage = Attacker.MaxHp;
        
        base.Constructor();
    }

    internal override void SetCursorPrice()
    {
        //only display sell price
        if (Ui.SelectedOption == (int)TowerManager.MenuOptions.Sell) Ui.CursorPrice = SellPrice;
        else Ui.CursorPrice = null;
    }

    internal override void Upgrade()
    {
        //Landmines cannot be upgraded
    }

    protected override void Fire(Attacker[] attackers)
    {
        //damage every attacker within range
        foreach (Attacker attacker in attackers)
        {
            Player.Money += Math.Min(attacker.Hp, Damage);
            attacker.Hp -= Damage;
        }
        
        //remove the tower
        UpdateTowerSpaceValidity(false);
        TowerManager.PlacedTowers.Remove(this);
    }
}

internal class Cannon : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerManager.CannonBase;
        TopTexture = TowerManager.CannonTop;
        BuyPrice = 30;
        TimeToFire = 3;
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
        BaseTexture = TowerManager.NailGunBase;
        BuyPrice = 75;
        TimeToFire = 2.5f;
        MinRange = 25;
        Damage = 1;

        base.Constructor();
    }
    
    protected override Attacker[] OrderAttackers(Attacker[] attackers) => attackers.OrderBy(_ => new Random().Next()).ToArray(); //the attackers that will be damaged will be randomly selected

    protected override void Fire(Attacker[] attackers)
    {
        //damage 4 random attackers
        for (int i = 0; i < Math.Min(attackers.Length, 4); i++)
        {
            Player.Money += Math.Min(attackers[i].Hp, Damage);
            attackers[i].Hp -= Damage;
        }
    }
}

internal class Sniper : Tower
{
    protected override void Constructor()
    {
        //set values
        BaseTexture = TowerManager.SniperBase;
        TopTexture = TowerManager.SniperTop;
        BuyPrice = 45;
        TimeToFire = 6;
        MinRange = 40;
        Damage = Attacker.MaxHp;

        base.Constructor();
    }

    protected override Attacker[] OrderAttackers(Attacker[] attackers) => attackers.OrderByDescending(x => x.Hp).ToArray(); //targets the towers with the highest health
}