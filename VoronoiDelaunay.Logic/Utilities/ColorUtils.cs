using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace VoronoiDelaunay.Logic.Utilities
{
    public static class ColorUtils
    {
        private static readonly Random Random;

        static ColorUtils()
        {
            Random = new Random();
        }

        public static Color GenerateRandomColor(Color mix)
        {
            int red = Random.Next(256);
            int green = Random.Next(256);
            int blue = Random.Next(256);

            red = (red + mix.R) / 2;
            green = (green + mix.G) / 2;
            blue = (blue + mix.B) / 2;

            Color color = Color.FromRgb((byte)red, (byte)green, (byte)blue);
            return color;
        }

        /// <summary>
        /// Brush colours only vary from shades of green and blue
        /// </summary>
        public static Color GenerateRandomBrushColour()
        {
            return Color.FromRgb(0, (byte)Random.Next(128, 256), (byte)Random.Next(128, 256));
        }

        public static List<Color> GenerateRandomColors(int max)
        {
            List<Color> colors = new List<Color>();

            for (int i = 0; i < max; i++)
            {
                colors.Add(GenerateRandomColor(Colors.Wheat));
            }

            return colors;
        }
    }
}