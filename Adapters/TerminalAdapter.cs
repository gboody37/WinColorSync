using System;
using System.IO;
using System.Text.RegularExpressions;
using WinColorSync.Core;

namespace WinColorSync.Adapters
{
    public static class TerminalAdapter
    {
        public static void ApplyToTerminal(ColorPalette palette)
        {
            if (palette == null) return;

            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string terminalPath = Path.Combine(localAppData, @"Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json");

                if (!File.Exists(terminalPath))
                {
                    terminalPath = Path.Combine(localAppData, @"Microsoft\Windows Terminal\settings.json");
                }

                if (File.Exists(terminalPath))
                {
                    string content = File.ReadAllText(terminalPath);

                    string schemeName = "WinColorSync";
                    string schemeJson = string.Format(@"
        {{
            ""name"": ""{0}"",
            ""background"": ""{1}"",
            ""foreground"": ""{2}"",
            ""selectionBackground"": ""{3}"",
            ""cursorColor"": ""{3}"",
            ""black"": ""#0C0C0C"",
            ""red"": ""#C50F1F"",
            ""green"": ""#13A10E"",
            ""yellow"": ""#C19C00"",
            ""blue"": ""{3}"",
            ""purple"": ""{4}"",
            ""cyan"": ""#3A96DD"",
            ""white"": ""#CCCCCC"",
            ""brightBlack"": ""#767676"",
            ""brightRed"": ""#E74856"",
            ""brightGreen"": ""#16C60C"",
            ""brightYellow"": ""#F9F1A5"",
            ""brightBlue"": ""{3}"",
            ""brightPurple"": ""{4}"",
            ""brightCyan"": ""#61D6D6"",
            ""brightWhite"": ""#F2F2F2""
        }}", schemeName, palette.DarkBackgroundHex, palette.ContrastTextHex, palette.PrimaryAccentHex, palette.SecondaryAccentHex);

                    if (content.Contains("\"name\": \"" + schemeName + "\""))
                    {
                        string pattern = @"\{\s*""name""\s*:\s*""WinColorSync""[^}]+\}";
                        content = Regex.Replace(content, pattern, schemeJson.Trim());
                    }
                    else if (content.Contains("\"schemes\": ["))
                    {
                        content = content.Replace("\"schemes\": [", "\"schemes\": [" + schemeJson + ",");
                    }

                    File.WriteAllText(terminalPath, content);
                    Console.WriteLine("[TerminalAdapter] Windows Terminal scheme updated.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TerminalAdapter] Error updating Terminal: " + ex.Message);
            }
        }
    }
}
