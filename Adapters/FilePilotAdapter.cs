using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using WinColorSync.Core;

namespace WinColorSync.Adapters
{
    public static class FilePilotAdapter
    {
        public static void ApplyToFilePilot(ColorPalette palette)
        {
            if (palette == null) return;

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configPath = Path.Combine(appData, @"Voidstar\FilePilot\FPilot-Config.json");

                if (File.Exists(configPath))
                {
                    string content = File.ReadAllText(configPath);

                    string bgHex = StripHash(palette.DarkBackgroundHex);
                    string surfaceHex = StripHash(palette.SurfaceAccentHex);
                    string accentHex = StripHash(palette.PrimaryAccentHex);
                    string secondaryHex = StripHash(palette.SecondaryAccentHex);
                    string textHex = StripHash(palette.ContrastTextHex);

                    // Update key colors in FPilot-Config.json
                    content = UpdateJsonColorKey(content, "Clear", bgHex);
                    content = UpdateJsonColorKey(content, "Caption", bgHex);
                    content = UpdateJsonColorKey(content, "Background", bgHex);
                    content = UpdateJsonColorKey(content, "AlternatingRow", bgHex);
                    content = UpdateJsonColorKey(content, "Surface", surfaceHex);
                    content = UpdateJsonColorKey(content, "Inner", surfaceHex);
                    content = UpdateJsonColorKey(content, "Border", secondaryHex);
                    content = UpdateJsonColorKey(content, "Outline", secondaryHex);
                    content = UpdateJsonColorKey(content, "Separator", secondaryHex);
                    content = UpdateJsonColorKey(content, "SurfaceSeparator", secondaryHex);
                    content = UpdateJsonColorKey(content, "IconTint", accentHex);
                    content = UpdateJsonColorKey(content, "Group", accentHex);
                    content = UpdateJsonColorKey(content, "Progress", accentHex);
                    content = UpdateJsonColorKey(content, "Selection", accentHex);
                    content = UpdateJsonColorKey(content, "RectSelection", accentHex);
                    content = UpdateJsonColorKey(content, "Match", accentHex);
                    content = UpdateJsonColorKey(content, "Foreground", textHex);
                    content = UpdateJsonColorKey(content, "File", textHex);
                    content = UpdateJsonColorKey(content, "Folder", textHex);

                    File.WriteAllText(configPath, content);
                    Console.WriteLine("[FilePilotAdapter] File Pilot FPilot-Config.json updated.");

                    // Restart FPilot.exe if currently running to apply colors instantly
                    Process[] processes = Process.GetProcessesByName("FPilot");
                    if (processes.Length > 0)
                    {
                        string execPath = processes[0].MainModule.FileName;
                        foreach (Process p in processes)
                        {
                            p.Kill();
                        }
                        if (File.Exists(execPath))
                        {
                            Process.Start(execPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FilePilotAdapter] Error updating File Pilot: " + ex.Message);
            }
        }

        private static string UpdateJsonColorKey(string json, string key, string hexValue)
        {
            string pattern = string.Format(@"(?<=`"{0}`"\s*:\s*`")[0-9A-Fa-f]{{6}}(?=`")", key).Replace('`', '"');
            return Regex.Replace(json, pattern, hexValue);
        }

        private static string StripHash(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return "000000";
            return hex.StartsWith("#") ? hex.Substring(1) : hex;
        }
    }
}
