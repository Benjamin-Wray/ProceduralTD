using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ProceduralTD
{
    internal class Perlin
    {
        private const int PTableLength = 256;

        private readonly int[] _pt;

        //array of pseudorandom vectors
        private readonly Vector2[] _gradients =
        {
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(-1, 0),
            new Vector2(0, -1),
            new Vector2(1, 1),
            new Vector2(1, -1),
            new Vector2(-1, 1),
            new Vector2(-1, -1)
        };

        public Perlin(int seed)
        {
            //create the permutation table we will use to select pseudorandom vectors
            _pt = GeneratePermutationTable(seed);
        }
        
        private static int[] GeneratePermutationTable(int seed)
        {
            int[] pt = new int[PTableLength]; //create empty permutation table
            for (int i = 0; i < PTableLength; i++) pt[i] = i; //fill table with values 0-255
            
            Random rng = new Random(seed); //pass seed into random number generator so values will always be randomised in the the same way
            pt = pt.OrderBy(_ => rng.Next()).ToArray(); //randomise order of values in table
            pt = pt.Concat(pt).ToArray(); //double table size to prevent IndexOutOfBounds error later

            return pt;
        }

        internal float Noise(float x, float y)
        {
            //find the unit square our coordinate is in
            // & PTableLength-1 makes sure the values are within the range of the permutation table
            int squareX = (int)Math.Floor(x) & PTableLength-1; 
            int squareY = (int)Math.Floor(y) & PTableLength-1;

            //calculate x and y coordinates within unit square
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);

            //select 4 pseudorandom vectors from permutation table using corners of unit square
            Vector2 v00 = _gradients[_pt[_pt[squareX] + squareY] % _gradients.Length];
            Vector2 v01 = _gradients[_pt[_pt[squareX] + squareY+1] % _gradients.Length];
            Vector2 v10 = _gradients[_pt[_pt[squareX+1] + squareY] % _gradients.Length];
            Vector2 v11 = _gradients[_pt[_pt[squareX+1] + squareY+1] % _gradients.Length];

            //find the dot products of the pseudorandom vectors and the vectors of  
            float dot00 = Vector2.Dot(v00, new Vector2(x, y));
            float dot01 = Vector2.Dot(v01, new Vector2(x, 1-y));
            float dot10 = Vector2.Dot(v10, new Vector2(1-x, y));
            float dot11 = Vector2.Dot(v11, new Vector2(1-x, 1-y));

            //smooth x and y coordinates by applying them to fade function for interpolation later
            float smoothedX = Fade(x);
            float smoothedY = Fade(y);
            
            
            float x1 = Lerp(dot00, dot01, smoothedX);
            float x2 = Lerp(dot10, dot11, smoothedX);

            float noiseValue = Lerp(x1, x2, smoothedY);
            
            return Normalise(noiseValue, -1, 1);
        }

        //applies value to the function 6t^5 - 15t^4 + 10t^3
        private float Fade(float t)
        {
            return (float) (6 * Math.Pow(t, 5) - 15 * Math.Pow(t, 4) + 10 * Math.Pow(t, 3));
        }
        
        //Linear interpolation
        private float Lerp(float dot1, float dot2, float smoothed)
        {
            return dot1 + smoothed * (dot2 - dot1);
        }

        //normalises value in the range 0.0 - 1.0
        private float Normalise(float x, float min, float max)
        {
            return (x - min) / (max - min);
        }
    }
}