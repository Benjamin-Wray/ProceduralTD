using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ProceduralTD
{
    internal static class Perlin
    {
        //permutation table (array of values from 0 to 255 used for selecting gradient vectors pseudorandomly)
        private static int[] _pt;
        private const int PTableLength = 256;

        //array of vectors that will be selected pseudorandomly
        private static readonly Vector2[] GradientVectors =
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
        
        private static int[] GeneratePermutationTable(int seed)
        {
            int[] pt = new int[PTableLength]; //create empty permutation table
            for (int i = 0; i < PTableLength; i++) pt[i] = i; //fill table with values 0-255
            
            Random rng = new Random(seed); //pass seed into random number generator so values will always be randomised in the the same way
            pt = pt.OrderBy(_ => rng.Next()).ToArray(); //randomise order of values in table
            pt = pt.Concat(pt).ToArray(); //double table size to prevent IndexOutOfBounds error later

            return pt;
        }

        internal static float[,] GenerateNoiseMap(int width, int height, float noiseScale, int octaves, float lacunarity, float persistence, int seed)
        {
            //creates a permutation table from seed
            _pt = GeneratePermutationTable(seed);

            //creates an empty noise map
            float[,] noiseMap = new float[width, height]; 
            
            //we record the lowest and highest values in the noise map so we can normalise them between 0.0 and 1.0
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //generate noise with octaves
                    float noise = OctaveNoise(x * noiseScale, y * noiseScale, octaves, lacunarity, persistence);
                    noiseMap[x, y] = noise;                    
                    
                    //update min and max values
                    if (noise < minValue) minValue = noise;
                    if (noise > maxValue) maxValue = noise;
                }
            }
            
            //normalise values in noise map
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[x, y] = Normalise(noiseMap[x, y], minValue, maxValue);
                }
            }

            return noiseMap;
        }

        //generate perlin noise in octaves
        private static float OctaveNoise(float x, float y, int octaves, float lacunarity, float persistence)
        {
            //initial values for frequency and amplitude
            float frequency = 1;
            float amplitude = 1;
            
            //initial value of the generated noise
            float noiseValue = 0;
            
            //loop through octaves 
            for (int octave = 0; octave < octaves; octave++)
            {
                //generate noise
                noiseValue += Noise(x * frequency, y * frequency) * amplitude;
                
                //update values for frequency and amplitude
                frequency *= lacunarity;
                amplitude *= persistence;
            }
            
            return noiseValue;
        }

        private static float Noise(float x, float y)
        {
            //find the unit square our coordinate is in
            // & PTableLength-1 makes sure the values are within the range of the permutation table
            int squareX = (int)Math.Floor(x) % PTableLength; 
            int squareY = (int)Math.Floor(y) % PTableLength;

            //calculate x and y coordinates within unit square (we will use these for our distance vectors when calculating dot products)
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);

            //select 4 pseudorandom vectors from permutation table using corners of unit square
            Vector2 v00 = GradientVectors[_pt[_pt[squareX] + squareY] % GradientVectors.Length];
            Vector2 v01 = GradientVectors[_pt[_pt[squareX] + squareY+1] % GradientVectors.Length];
            Vector2 v10 = GradientVectors[_pt[_pt[squareX+1] + squareY] % GradientVectors.Length];
            Vector2 v11 = GradientVectors[_pt[_pt[squareX+1] + squareY+1] % GradientVectors.Length];

            //find the dot products of the pseudorandom vectors and the distance vectors (vectors of the corners of the square to the point)
            float dot00 = Vector2.Dot(v00, new Vector2(x, y));
            float dot01 = Vector2.Dot(v01, new Vector2(x, y-1));
            float dot10 = Vector2.Dot(v10, new Vector2(x-1, y));
            float dot11 = Vector2.Dot(v11, new Vector2(x-1, y-1));

            //smooth x and y values by applying them to fade function for interpolation
            float smoothedX = Fade(x);
            float smoothedY = Fade(y);
            
            //linearly interpolates between our dot products using the smoothed values
            float x0 = Lerp(dot00, dot01, smoothedY);
            float x1 = Lerp(dot10, dot11, smoothedY);
            float noiseValue = Lerp(x0, x1, smoothedX);
            
            return noiseValue;
        }
        
        //applies value to the function 6t^5 - 15t^4 + 10t^3
        //this is used to make interpolation smoother
        private static float Fade(float t)
        {
            return (float)(6 * Math.Pow(t, 5) - 15 * Math.Pow(t, 4) + 10 * Math.Pow(t, 3));
        }
        
        //linearly interpolates between two dot products using smoothed value from the fade function
        private static float Lerp(float dot1, float dot2, float smoothed)
        {
            return dot1 + smoothed * (dot2 - dot1);
        }
        
        //normalises value in the range 0.0 - 1.0
        private static float Normalise(float x, float min, float max)
        {
            return (x - min) / (max - min);
        }
    }
}