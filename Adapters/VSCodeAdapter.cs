using System;
using System.IO;
using System.Text.RegularExpressions;
using WinColorSync.Core;

namespace WinColorSync.Adapters
{
    public static class VSCodeAdapter
    {
        public static void ApplyToVSCode(ColorPalette palette)
        {
            if (palette == null) return;

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string vscodePath = Path.Combine(appData, @"Code\User\settings.json");

                if (File.Exists(vscodePath))
                {
                    string content = File.ReadAllText(vscodePath);

                    string customizations = string.Format(@"""workbench.colorCustomizations"": {{
    ""activityBar.background"": ""{0}"",
    ""activityBar.activeBorder"": ""{1}"",
    ""activityBar.foreground"": ""{1}"",
    ""statusBar.background"": ""{1}"",
    ""statusBar.foreground"": ""#FFFFFF"",
    ""titleBar.activeBackground"": ""{2}"",
    ""titleBar.activeForeground"": ""{3}"",
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
                        if (lastBrace > 0)
                        {
                            content = content.Insert(lastBrace, ",\n" + customizations + "\n");
                        }
                    }

                    File.WriteAllText(vscodePath, content);
                    Console.WriteLine("[VSCodeAdapter] VS Code theme updated.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VSCodeAdapter] Error updating VS Code: " + ex.Message);
            }
        }
    }
}
