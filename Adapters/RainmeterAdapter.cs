using System;
using System.Diagnostics;
using System.IO;
using WinColorSync.Core;

namespace WinColorSync.Adapters
{
    public static class RainmeterAdapter
    {
        public static void ApplyToRainmeter(ColorPalette palette)
        {
            if (palette == null) return;

            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string rainmeterVarPath = Path.Combine(docs, @"Rainmeter\Skins\#@#Variables.inc");

                if (File.Exists(rainmeterVarPath))
                {
                    string content = File.ReadAllText(rainmeterVarPath);

                    string r = palette.PrimaryAccent.R.ToString();
                    string g = palette.PrimaryAccent.G.ToString();
                    string b = palette.PrimaryAccent.B.ToString();

                    content += string.Format("\n; WinColorSync Generated\nAccentColor={0},{1},{2}\nAccentHex={3}\n", r, g, b, palette.PrimaryAccentHex);
                    File.WriteAllText(rainmeterVarPath, content);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "rainmeter.exe",
                        Arguments = "!RefreshApp",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                    Console.WriteLine("[RainmeterAdapter] Rainmeter skins refreshed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[RainmeterAdapter] Rainmeter sync notice: " + ex.Message);
            }
        }
    }
}
