using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ProceduralTD
{
    internal static class MapGenerator
    {
        internal static float[,] NoiseMap;
        
        //permutation table (array of values from 0 to 255 used for selecting gradient vectors pseudorandomly)
        private const int PTableLength = 256;

        //array of vectors that will be selected pseudorandomly
        private static readonly Vector2[] GradientVectors =
        {
            new(1, 1),
            new(1, -1),
            new(-1, 1),
            new(-1, -1)
        };
        
        //values for map generation
        private const int MapScale = 10; //how many times bigger the map is than the camera
        internal const int MapWidth = Camera.CameraWidth * MapScale;
        internal const int MapHeight = Camera.CameraHeight * MapScale;
        private const float NoiseScale = .008f; //how "zoomed in" the noise is
        private const int Octaves = 4; //how many times noise will be generated and layered on top of each other
        private const float Lacunarity = 2; //this is multiplied by the frequency of the noise every octave
        private const float Persistence = .5f; //this is multiplied by the amplitude of the noise every octave

        internal static void GenerateNoiseMap(int seed)
        {
            //creates a permutation table from seed
            int[] pt = GeneratePermutationTable(seed);

            //creates an empty noise map
            NoiseMap = new float[MapWidth, MapHeight];

            Parallel.For(0, MapHeight, y =>
            {
                Parallel.For(0, MapWidth, x =>
                {
                    //generate noise with octaves
                    NoiseMap[x, y] = OctaveNoise(x * NoiseScale, y * NoiseScale, pt);
                });
            });

            NoiseMap = NormalizeMap(NoiseMap);

            StateMachine.ChangeState(StateMachine.Action.BeginGame);
        }

        private static int[] GeneratePermutationTable(int seed)
        {
            Random rng = new Random(seed); //pass seed into random number generator so values will always be randomised in the the same way
            int[] permutationTable = new int[PTableLength]; //create empty permutation table

            //place every number from 1-255 in the table
            for (int i = 1; i < PTableLength; i++)
            {
                int pos = rng.Next(PTableLength); //generate a random position to place the number in the table
                while (permutationTable[pos] != 0) pos = (pos + 1) % PTableLength; //if there is already a number in that position check the next position until an empty space is found
                permutationTable[pos] = i; //insert the value into the random position in the table
            }

            return permutationTable;
        }

        //generate perlin noise in octaves
        private static float OctaveNoise(float x, float y, int[] pt)
        {
            //initial value of the generated noise
            float noiseValue = 0;
            
            //loop through octaves 
            for (int octave = 0; octave < Octaves; octave++)
            {
                float frequency = (float)Math.Pow(Lacunarity, octave);
                float amplitude = (float)Math.Pow(Persistence, octave);
                
                //generate noise and add it to the total noise value
                noiseValue += Noise(x * frequency, y * frequency, pt) * amplitude;
            }
            
            return noiseValue;
        }

        private static float Noise(float x, float y, int[] pt)
        {
            //find the unit square our coordinate is in
            // MOD PTableLength-1 makes sure the values are within the range of the permutation table (0-255)
            int squareX = (int)Math.Floor(x) % PTableLength;
            int squareY = (int)Math.Floor(y) % PTableLength;
            
            //select 4 pseudorandom vectors from permutation table using corners of unit square
            Vector2 vec00 = GradientVectors[pt[(pt[squareX] + pt[squareY]) % pt.Length] % GradientVectors.Length];
            Vector2 vec01 = GradientVectors[pt[(pt[squareX] + pt[squareY+1]) % pt.Length] % GradientVectors.Length];
            Vector2 vec10 = GradientVectors[pt[(pt[squareX+1] + pt[squareY]) % pt.Length] % GradientVectors.Length];
            Vector2 vec11 = GradientVectors[pt[(pt[squareX+1] + pt[squareY+1]) % pt.Length] % GradientVectors.Length];

            //calculate x and y coordinates within unit square (we will use these for our distance vectors when calculating dot products)
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            
            //find the dot products of the pseudorandom vectors and the distance vectors (vectors of the corners of the square to the point)
            float dot00 = Vector2.Dot(vec00, new Vector2(x, y));
            float dot01 = Vector2.Dot(vec01, new Vector2(x, y-1));
            float dot10 = Vector2.Dot(vec10, new Vector2(x-1, y));
            float dot11 = Vector2.Dot(vec11, new Vector2(x-1, y-1));
            
            //smooth x and y values by applying them to fade function for interpolation
            float smoothedX = Fade(x);
            float smoothedY = Fade(y);
            
            //linearly interpolates between our dot products using the smoothed values
            float x0 = Lerp(dot00, dot10, smoothedX);
            float x1 = Lerp(dot01, dot11, smoothedX);
            float noiseValue = Lerp(x0, x1, smoothedY);
            
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
        internal static float Normalize(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        private static float[,] NormalizeMap(float[,] noiseMap)
        {
            //get minimum and maximum values in the noise map
            float minNoiseValue = noiseMap.Cast<float>().Min();
            float maxNoiseValue = noiseMap.Cast<float>().Max();

            Parallel.For(0, MapHeight, y =>
            {
                Parallel.For(0, MapWidth, x =>
                {
                    //generate noise with octaves
                    noiseMap[x, y] = Normalize(noiseMap[x, y], minNoiseValue, maxNoiseValue);
                });
            });

            return noiseMap;
        }
    }
}