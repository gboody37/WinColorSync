using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace WinColorSync.Core
{
    public class ColorPalette
    {
        public Color PrimaryAccent { get; set; }
        public Color SecondaryAccent { get; set; }
        public Color DarkBackground { get; set; }
        public Color LightBackground { get; set; }
        public Color SurfaceAccent { get; set; }
        public Color ContrastText { get; set; }
        public bool IsDarkThemeRecommended { get; set; }

        public string PrimaryAccentHex
        {
            get { return ColorToHex(PrimaryAccent); }
        }

        public string SecondaryAccentHex
        {
            get { return ColorToHex(SecondaryAccent); }
        }

        public string DarkBackgroundHex
        {
            get { return ColorToHex(DarkBackground); }
        }

        public string LightBackgroundHex
        {
            get { return ColorToHex(LightBackground); }
        }

        public string SurfaceAccentHex
        {
            get { return ColorToHex(SurfaceAccent); }
        }

        public string ContrastTextHex
        {
            get { return ColorToHex(ContrastText); }
        }

        private static string ColorToHex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }
    }

    public static class ColorExtractor
    {
        public static ColorPalette ExtractPalette(Bitmap originalBitmap)
        {
            if (originalBitmap == null)
            {
                return GetDefaultPalette();
            }

            int targetWidth = 100;
            int targetHeight = (int)((double)originalBitmap.Height / originalBitmap.Width * targetWidth);
            if (targetHeight <= 0) targetHeight = 100;

            using (Bitmap resized = new Bitmap(originalBitmap, new Size(targetWidth, targetHeight)))
            {
                List<Color> pixels = new List<Color>();
                for (int y = 0; y < resized.Height; y += 2)
                {
                    for (int x = 0; x < resized.Width; x += 2)
                    {
                        Color c = resized.GetPixel(x, y);
                        double saturation = GetSaturation(c);
                        double brightness = GetBrightness(c);

                        if (brightness > 0.05 && brightness < 0.95)
                        {
                            pixels.Add(c);
                        }
                    }
                }

                if (pixels.Count == 0)
                {
                    return GetDefaultPalette();
                }

                List<Color> centers = RunKMeans(pixels, 4, 10);
                centers.Sort((a, b) => GetSaturation(b).CompareTo(GetSaturation(a)));

                Color primary = centers.Count > 0 ? centers[0] : Color.FromArgb(0, 120, 215);
                Color secondary = centers.Count > 1 ? centers[1] : Darken(primary, 0.2);

                double totalBrightness = 0;
                foreach (Color p in pixels)
                {
                    totalBrightness += GetBrightness(p);
                }
                double avgBrightness = totalBrightness / pixels.Count;
                bool recommendDark = avgBrightness < 0.6;

                Color darkBg = Color.FromArgb(24, 24, 28);
                Color lightBg = Color.FromArgb(245, 245, 248);
                Color surface = recommendDark ? Darken(primary, 0.6) : Lighten(primary, 0.7);
                Color contrastText = recommendDark ? Color.White : Color.Black;

                return new ColorPalette
                {
                    PrimaryAccent = primary,
                    SecondaryAccent = secondary,
                    DarkBackground = darkBg,
                    LightBackground = lightBg,
                    SurfaceAccent = surface,
                    ContrastText = contrastText,
                    IsDarkThemeRecommended = recommendDark
                };
            }
        }

        private static List<Color> RunKMeans(List<Color> pixels, int k, int iterations)
        {
            List<Color> centers = new List<Color>();
            Random rand = new Random();

            for (int i = 0; i < k; i++)
            {
                centers.Add(pixels[rand.Next(pixels.Count)]);
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                List<List<Color>> clusters = new List<List<Color>>();
                for (int i = 0; i < k; i++) clusters.Add(new List<Color>());

                foreach (Color p in pixels)
                {
                    int bestIdx = 0;
                    double minDistance = double.MaxValue;
                    for (int i = 0; i < k; i++)
                    {
                        double dist = ColorDistance(p, centers[i]);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestIdx = i;
                        }
                    }
                    clusters[bestIdx].Add(p);
                }

                for (int i = 0; i < k; i++)
                {
                    if (clusters[i].Count > 0)
                    {
                        long r = 0, g = 0, b = 0;
                        foreach (Color c in clusters[i])
                        {
                            r += c.R; g += c.G; b += c.B;
                        }
                        centers[i] = Color.FromArgb((int)(r / clusters[i].Count), (int)(g / clusters[i].Count), (int)(b / clusters[i].Count));
                    }
                }
            }

            return centers;
        }

        private static double ColorDistance(Color c1, Color c2)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        public static double GetSaturation(Color c)
        {
            int max = Math.Max(c.R, Math.Max(c.G, c.B));
            int min = Math.Min(c.R, Math.Min(c.G, c.B));
            if (max == 0) return 0;
            return (double)(max - min) / max;
        }

        public static double GetBrightness(Color c)
        {
            return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
        }

        public static Color Darken(Color c, double factor)
        {
            return Color.FromArgb(
                (int)(c.R * (1 - factor)),
                (int)(c.G * (1 - factor)),
                (int)(c.B * (1 - factor))
            );
        }

        public static Color Lighten(Color c, double factor)
        {
            return Color.FromArgb(
                (int)(c.R + (255 - c.R) * factor),
                (int)(c.G + (255 - c.G) * factor),
                (int)(c.B + (255 - c.B) * factor)
            );
        }

        public static ColorPalette GetDefaultPalette()
        {
            return new ColorPalette
            {
                PrimaryAccent = Color.FromArgb(0, 120, 215),
                SecondaryAccent = Color.FromArgb(0, 90, 160),
                DarkBackground = Color.FromArgb(24, 24, 28),
                LightBackground = Color.FromArgb(245, 245, 248),
                SurfaceAccent = Color.FromArgb(32, 40, 50),
                ContrastText = Color.White,
                IsDarkThemeRecommended = true
            };
        }
    }
}
