using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinColorSync.Core
{
    public class ColorPalette
    {
        public Color PrimaryAccent { get; set; }
        public Color SecondaryAccent { get; set; }
        public Color DarkBackground { get; set; }
        public Color LightBackground { get; set; }
        public Color SurfaceAccent { get; set; }
        public Color WindowBorder { get; set; }
        public Color ContrastText { get; set; }
        public bool IsDarkThemeRecommended { get; set; }

        public string PrimaryAccentHex
        {
            get { return ColorToHex(PrimaryAccent); }
            set { PrimaryAccent = HexToColor(value, PrimaryAccent); }
        }

        public string SecondaryAccentHex
        {
            get { return ColorToHex(SecondaryAccent); }
            set { SecondaryAccent = HexToColor(value, SecondaryAccent); }
        }

        public string DarkBackgroundHex
        {
            get { return ColorToHex(DarkBackground); }
            set { DarkBackground = HexToColor(value, DarkBackground); }
        }

        public string LightBackgroundHex
        {
            get { return ColorToHex(LightBackground); }
            set { LightBackground = HexToColor(value, LightBackground); }
        }

        public string SurfaceAccentHex
        {
            get { return ColorToHex(SurfaceAccent); }
            set { SurfaceAccent = HexToColor(value, SurfaceAccent); }
        }

        public string WindowBorderHex
        {
            get { return ColorToHex(WindowBorder); }
            set { WindowBorder = HexToColor(value, WindowBorder); }
        }

        public string ContrastTextHex
        {
            get { return ColorToHex(ContrastText); }
            set { ContrastText = HexToColor(value, ContrastText); }
        }

        public static string ColorToHex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        public static Color HexToColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex)) return defaultColor;
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                try
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
                catch { }
            }
            return defaultColor;
        }
    }

    public static class ColorExtractor
    {
        public static ColorPalette ExtractPalette(Bitmap originalBitmap)
        {
            // Catppuccin Mocha Official Palette Defaults
            Color mochaBase = Color.FromArgb(30, 30, 46);       // #1E1E2E
            Color mochaMantle = Color.FromArgb(24, 24, 37);     // #181825
            Color mochaLavender = Color.FromArgb(180, 190, 254); // #B4BEFE
            Color mochaBlue = Color.FromArgb(137, 180, 250);     // #89B4FA
            Color mochaSurface0 = Color.FromArgb(49, 50, 68);    // #313244
            Color mochaText = Color.FromArgb(205, 214, 244);     // #CDD6F4

            if (originalBitmap == null)
            {
                return GetDefaultPalette();
            }

            int targetWidth = 100;
            int targetHeight = (int)((double)originalBitmap.Height / originalBitmap.Width * targetWidth);
            if (targetHeight <= 0) targetHeight = 100;

            using (Bitmap resized = new Bitmap(originalBitmap, new Size(targetWidth, targetHeight)))
            {
                long rTot = 0, gTot = 0, bTot = 0, count = 0;
                double maxSat = 0;
                Color bestAccent = mochaLavender;

                for (int y = 0; y < resized.Height; y += 2)
                {
                    for (int x = 0; x < resized.Width; x += 2)
                    {
                        Color c = resized.GetPixel(x, y);
                        rTot += c.R; gTot += c.G; bTot += c.B; count++;

                        double sat = GetSaturation(c);
                        if (sat > maxSat && (c.B > c.G || c.R > c.G || sat > 0.35))
                        {
                            maxSat = sat;
                            bestAccent = c;
                        }
                    }
                }

                if (count == 0) return GetDefaultPalette();

                int avgR = (int)(rTot / count);
                int avgG = (int)(gTot / count);
                int avgB = (int)(bTot / count);

                Color extractedBg = Color.FromArgb(
                    Math.Min(32, (int)(avgR * 0.22)),
                    Math.Min(32, (int)(avgG * 0.22)),
                    Math.Min(48, (int)(avgB * 0.30 + 10))
                );

                return new ColorPalette
                {
                    PrimaryAccent = bestAccent,
                    SecondaryAccent = mochaBlue,
                    DarkBackground = extractedBg.R == 0 && extractedBg.G == 0 && extractedBg.B == 0 ? mochaBase : extractedBg,
                    LightBackground = Color.FromArgb(245, 245, 248),
                    SurfaceAccent = mochaMantle,
                    WindowBorder = bestAccent,
                    ContrastText = mochaText,
                    IsDarkThemeRecommended = true
                };
            }
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

        public static ColorPalette GetDefaultPalette()
        {
            return new ColorPalette
            {
                PrimaryAccent = Color.FromArgb(180, 190, 254), // #B4BEFE Catppuccin Lavender
                SecondaryAccent = Color.FromArgb(137, 180, 250), // #89B4FA Catppuccin Blue
                DarkBackground = Color.FromArgb(30, 30, 46),    // #1E1E2E Catppuccin Base
                LightBackground = Color.FromArgb(245, 245, 248),
                SurfaceAccent = Color.FromArgb(24, 24, 37),    // #181825 Catppuccin Mantle
                WindowBorder = Color.FromArgb(180, 190, 254),
                ContrastText = Color.FromArgb(205, 214, 244),  // #CDD6F4 Catppuccin Text
                IsDarkThemeRecommended = true
            };
        }
    }
}
