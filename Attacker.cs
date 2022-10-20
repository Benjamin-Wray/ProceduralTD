using Microsoft.Xna.Framework;
using Point = System.Drawing.Point;

namespace ProceduralTD;

internal class Attacker
{
    private Point _position;
    private Point Position
    {
        get => _position;
        set => _position = value;
    }

    internal Attacker(Point position)
    {
        Position = position;
    }
    
    internal void Update(GameTime gameTime)
    {
        
    }

    internal void Draw()
    {
        
    }
}