using System;
using System.IO;
using System.Text.RegularExpressions;
using WinColorSync.Core;

namespace WinColorSync.Adapters
{
    public static class AntigravityAdapter
    {
        public static void ApplyToAntigravity(ColorPalette palette)
        {
            if (palette == null) return;

            try
            {
                // 1. Export theme.css & theme.json for Antigravity UI
                ThemeExporter.ExportThemeFiles(palette);

                // 2. Update Antigravity user settings JSON if present (%APPDATA%\Antigravity\User\settings.json)
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string antigravitySettings = Path.Combine(appData, @"Antigravity\User\settings.json");

                if (!File.Exists(antigravitySettings))
                {
                    string userDir = Path.Combine(appData, @"Antigravity\User");
                    if (!Directory.Exists(userDir))
                    {
                        Directory.CreateDirectory(userDir);
                    }
                    File.WriteAllText(antigravitySettings, "{\n}");
                }

                if (File.Exists(antigravitySettings))
                {
                    string content = File.ReadAllText(antigravitySettings);

                    string customizations = string.Format(@"""workbench.colorCustomizations"": {{
    ""activityBar.background"": ""{0}"",
    ""activityBar.activeBorder"": ""{1}"",
    ""activityBar.foreground"": ""{1}"",
    ""statusBar.background"": ""{1}"",
    ""statusBar.foreground"": ""#FFFFFF"",
    ""titleBar.activeBackground"": ""{2}"",
    ""titleBar.activeForeground"": ""{3}"",
    ""sideBar.background"": ""{0}"",
    ""editor.background"": ""{0}"",
    ""selection.background"": ""{1}80"",
    ""editor.selectionHighlightBackground"": ""{4}40""
}}", palette.DarkBackgroundHex, palette.PrimaryAccentHex, palette.SurfaceAccentHex, palette.ContrastTextHex, palette.SecondaryAccentHex);

                    if (content.Contains("\"workbench.colorCustomizations\""))
                    {
                        string pattern = @"""workbench\.colorCustomizations""\s*:\s*\{[^}]+\}";
                        content = Regex.Replace(content, pattern, customizations);
                    }
                    else
                    {
                        int lastBrace = content.LastIndexOf('}');
                        if (lastBrace >= 0)
                        {
                            content = content.Insert(lastBrace, (content.Trim().Length > 2 ? ",\n" : "") + customizations + "\n");
                        }
                    }

                    File.WriteAllText(antigravitySettings, content);
                    Console.WriteLine("[AntigravityAdapter] Antigravity theme updated.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AntigravityAdapter] Notice: " + ex.Message);
            }
        }
    }
}
