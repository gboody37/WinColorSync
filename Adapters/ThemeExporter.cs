using System;
using System.IO;
using WinColorSync.Core;

namespace WinColorSync.Adapters
{
    public static class ThemeExporter
    {
        public static void ExportThemeFiles(ColorPalette palette)
        {
            if (palette == null) return;

            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string exportDir = Path.Combine(userProfile, ".wincolorsync");

                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                string cssContent = string.Format(@"/* WinColorSync Auto-Generated CSS Theme Variables */
:root {{
    --primary-accent: {0};
    --secondary-accent: {1};
    --dark-background: {2};
    --light-background: {3};
    --surface-accent: {4};
    --contrast-text: {5};
    --is-dark-theme: {6};
}}
", palette.PrimaryAccentHex, palette.SecondaryAccentHex, palette.DarkBackgroundHex, palette.LightBackgroundHex, palette.SurfaceAccentHex, palette.ContrastTextHex, palette.IsDarkThemeRecommended ? "true" : "false");

                File.WriteAllText(Path.Combine(exportDir, "theme.css"), cssContent);

                string jsonContent = string.Format(@"{{
    ""primaryAccent"": ""{0}"",
    ""secondaryAccent"": ""{1}"",
    ""darkBackground"": ""{2}"",
    ""lightBackground"": ""{3}"",
    ""surfaceAccent"": ""{4}"",
    ""contrastText"": ""{5}"",
    ""isDarkThemeRecommended"": {6}
}}", palette.PrimaryAccentHex, palette.SecondaryAccentHex, palette.DarkBackgroundHex, palette.LightBackgroundHex, palette.SurfaceAccentHex, palette.ContrastTextHex, palette.IsDarkThemeRecommended ? "true" : "false");

                File.WriteAllText(Path.Combine(exportDir, "theme.json"), jsonContent);

                Console.WriteLine("[ThemeExporter] Exported theme files to " + exportDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ThemeExporter] Error exporting theme files: " + ex.Message);
            }
        }
    }
}
