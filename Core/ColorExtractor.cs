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

    public class ClusterInfo
    {
        public Color Center { get; set; }
        public int Count { get; set; }
        public double Saturation { get; set; }
        public double Brightness { get; set; }
    }

    public static class ColorExtractor
    {
        public static ColorPalette ExtractPalette(Bitmap originalBitmap)
        {
            if (originalBitmap == null)
            {
                return GetDefaultPalette();
            }

            int targetWidth = 120;
            int targetHeight = (int)((double)originalBitmap.Height / originalBitmap.Width * targetWidth);
            if (targetHeight <= 0) targetHeight = 120;

            using (Bitmap resized = new Bitmap(originalBitmap, new Size(targetWidth, targetHeight)))
            {
                List<Color> pixels = new List<Color>();
                double totalBrightness = 0;

                for (int y = 0; y < resized.Height; y += 2)
                {
                    for (int x = 0; x < resized.Width; x += 2)
                    {
                        Color c = resized.GetPixel(x, y);
                        pixels.Add(c);
                        totalBrightness += GetBrightness(c);
                    }
                }

                if (pixels.Count == 0)
                {
                    return GetDefaultPalette();
                }

                double avgBrightness = totalBrightness / pixels.Count;
                bool recommendDark = avgBrightness < 0.6;

                // Run K-Means with 6 clusters
                List<ClusterInfo> clusters = RunKMeansWithFrequency(pixels, 6, 12);

                // Sort clusters by frequency (most dominant first)
                clusters.Sort((a, b) => b.Count.CompareTo(a.Count));

                // 1. Dominant background color (most frequent cluster)
                Color dominantBg = clusters.Count > 0 ? clusters[0].Center : Color.FromArgb(24, 24, 28);
                Color darkBg = recommendDark ? DarkenToBackground(dominantBg) : Color.FromArgb(24, 24, 28);
                Color lightBg = !recommendDark ? LightenToBackground(dominantBg) : Color.FromArgb(245, 245, 248);

                // 2. Primary Accent: Most vibrant cluster (highest saturation)
                List<ClusterInfo> vibrantClusters = new List<ClusterInfo>(clusters);
                vibrantClusters.Sort((a, b) => b.Saturation.CompareTo(a.Saturation));

                Color primary = Color.FromArgb(0, 120, 215);
                Color secondary = Color.FromArgb(0, 90, 160);

                if (vibrantClusters.Count > 0 && vibrantClusters[0].Saturation > 0.1)
                {
                    primary = vibrantClusters[0].Center;
                }
                else if (clusters.Count > 0)
                {
                    primary = clusters[0].Center;
                }

                if (vibrantClusters.Count > 1 && vibrantClusters[1].Saturation > 0.08)
                {
                    secondary = vibrantClusters[1].Center;
                }
                else
                {
                    secondary = Darken(primary, 0.25);
                }

                // 3. Surface Accent: Blend of background and primary accent
                Color surface = BlendColors(darkBg, primary, 0.15);
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

        private static List<ClusterInfo> RunKMeansWithFrequency(List<Color> pixels, int k, int iterations)
        {
            List<Color> centers = new List<Color>();
            Random rand = new Random();

            for (int i = 0; i < k; i++)
            {
                centers.Add(pixels[rand.Next(pixels.Count)]);
            }

            List<List<Color>> clusters = new List<List<Color>>();

            for (int iter = 0; iter < iterations; iter++)
            {
                clusters = new List<List<Color>>();
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

            List<ClusterInfo> result = new List<ClusterInfo>();
            for (int i = 0; i < k; i++)
            {
                Color c = centers[i];
                result.Add(new ClusterInfo
                {
                    Center = c,
                    Count = clusters[i].Count,
                    Saturation = GetSaturation(c),
                    Brightness = GetBrightness(c)
                });
            }

            return result;
        }

        private static Color DarkenToBackground(Color c)
        {
            double brightness = GetBrightness(c);
            if (brightness > 0.25)
            {
                double factor = 0.25 / brightness;
                return Color.FromArgb(
                    (int)(c.R * factor),
                    (int)(c.G * factor),
                    (int)(c.B * factor)
                );
            }
            return c;
        }

        private static Color LightenToBackground(Color c)
        {
            double brightness = GetBrightness(c);
            if (brightness < 0.85)
            {
                return Lighten(c, 0.5);
            }
            return c;
        }

        private static Color BlendColors(Color baseColor, Color overlay, double amount)
        {
            int r = (int)(baseColor.R * (1 - amount) + overlay.R * amount);
            int g = (int)(baseColor.G * (1 - amount) + overlay.G * amount);
            int b = (int)(baseColor.B * (1 - amount) + overlay.B * amount);
            return Color.FromArgb(
                Math.Min(255, Math.Max(0, r)),
                Math.Min(255, Math.Max(0, g)),
                Math.Min(255, Math.Max(0, b))
            );
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
