using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WinColorSync.Core
{
    public class WallpaperEngineWatcher
    {
        public event EventHandler<Bitmap> WallpaperChanged;

        private FileSystemWatcher _configWatcher;
        private FileSystemWatcher _workshopWatcher;
        private Timer _pollTimer;
        private string _customWallpaperEnginePath;
        private string _lastWallpaperPath;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_GETDESKWALLPAPER = 0x0073;

        public WallpaperEngineWatcher(string customPath = null)
        {
            _customWallpaperEnginePath = customPath;
        }

        public void Start()
        {
            string wpPath = GetWallpaperEnginePath();

            if (!string.IsNullOrEmpty(wpPath) && Directory.Exists(wpPath))
            {
                try
                {
                    _configWatcher = new FileSystemWatcher(wpPath)
                    {
                        Filter = "config.json",
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                    };
                    _configWatcher.Changed += OnFileOrDirChanged;
                    _configWatcher.EnableRaisingEvents = true;
                }
                catch { }

                string workshopPath = GetSteamWorkshopPath(wpPath);
                if (!string.IsNullOrEmpty(workshopPath) && Directory.Exists(workshopPath))
                {
                    try
                    {
                        _workshopWatcher = new FileSystemWatcher(workshopPath)
                        {
                            IncludeSubdirectories = true,
                            Filter = "*.*",
                            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                        };
                        _workshopWatcher.Changed += OnFileOrDirChanged;
                        _workshopWatcher.Created += OnFileOrDirChanged;
                        _workshopWatcher.EnableRaisingEvents = true;
                    }
                    catch { }
                }
            }

            _pollTimer = new Timer();
            _pollTimer.Interval = 2000;
            _pollTimer.Tick += (s, e) => CheckCurrentWallpaper();
            _pollTimer.Start();

            CheckCurrentWallpaper();
        }

        public void Stop()
        {
            if (_configWatcher != null)
            {
                _configWatcher.EnableRaisingEvents = false;
                _configWatcher.Dispose();
                _configWatcher = null;
            }
            if (_workshopWatcher != null)
            {
                _workshopWatcher.EnableRaisingEvents = false;
                _workshopWatcher.Dispose();
                _workshopWatcher = null;
            }
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }

        private void OnFileOrDirChanged(object sender, FileSystemEventArgs e)
        {
            CheckCurrentWallpaper();
        }

        public Bitmap CheckCurrentWallpaper()
        {
            try
            {
                string wpImage = ParseActiveWallpaperFromConfig();

                if (string.IsNullOrEmpty(wpImage) || !File.Exists(wpImage))
                {
                    wpImage = FindLatestWorkshopOrProjectPreview();
                }

                if (string.IsNullOrEmpty(wpImage) || !File.Exists(wpImage))
                {
                    wpImage = GetWindowsNativeWallpaperPath();
                }

                if (!string.IsNullOrEmpty(wpImage) && File.Exists(wpImage))
                {
                    string fileKey = wpImage + "_" + File.GetLastWriteTime(wpImage).Ticks;
                    if (fileKey != _lastWallpaperPath)
                    {
                        _lastWallpaperPath = fileKey;
                        using (FileStream stream = new FileStream(wpImage, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            Bitmap bmp = new Bitmap(stream);
                            Bitmap copy = new Bitmap(bmp);
                            if (WallpaperChanged != null)
                            {
                                WallpaperChanged(this, copy);
                            }
                            return copy;
                        }
                    }
                    else
                    {
                        // If same path requested manually (e.g. Sync Now button click)
                        using (FileStream stream = new FileStream(wpImage, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            Bitmap bmp = new Bitmap(stream);
                            return new Bitmap(bmp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WallpaperEngineWatcher] Error checking wallpaper: " + ex.Message);
            }

            return null;
        }

        private string ParseActiveWallpaperFromConfig()
        {
            string basePath = GetWallpaperEnginePath();
            if (string.IsNullOrEmpty(basePath)) return null;

            string configPath = Path.Combine(basePath, "config.json");
            if (!File.Exists(configPath)) return null;

            try
            {
                string json = File.ReadAllText(configPath);

                // Find active selectedwallpapers block for Monitor0 / active display
                Match match = Regex.Match(json, @"""selectedwallpapers""\s*:\s*\{[^}]*""file""\s*:\s*""([^""]+)""", RegexOptions.Singleline | RegexOptions.RightToLeft);
                if (match.Success)
                {
                    string filePath = match.Groups[1].Value.Replace('/', '\\');
                    string dirPath = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(dirPath))
                    {
                        string preview = FindPreviewInDir(dirPath);
                        if (!string.IsNullOrEmpty(preview)) return preview;
                    }
                }

                // Fallback: search all file entries from bottom of config.json
                MatchCollection matches = Regex.Matches(json, @"""file""\s*:\s*""([^""]+)""");
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    string filePath = matches[i].Groups[1].Value.Replace('/', '\\');
                    string dirPath = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(dirPath))
                    {
                        string preview = FindPreviewInDir(dirPath);
                        if (!string.IsNullOrEmpty(preview)) return preview;
                    }
                }
            }
            catch { }

            return null;
        }

        private string FindLatestWorkshopOrProjectPreview()
        {
            string basePath = GetWallpaperEnginePath();
            if (string.IsNullOrEmpty(basePath)) return null;

            string workshopPath = GetSteamWorkshopPath(basePath);
            DateTime newest = DateTime.MinValue;
            string newestFile = null;

            if (!string.IsNullOrEmpty(workshopPath) && Directory.Exists(workshopPath))
            {
                foreach (string subDir in Directory.GetDirectories(workshopPath))
                {
                    string preview = FindPreviewInDir(subDir);
                    if (!string.IsNullOrEmpty(preview))
                    {
                        DateTime writeTime = File.GetLastWriteTime(preview);
                        if (writeTime > newest)
                        {
                            newest = writeTime;
                            newestFile = preview;
                        }
                    }
                }
            }

            return newestFile;
        }

        private string FindPreviewInDir(string dir)
        {
            if (!Directory.Exists(dir)) return null;

            string[] candidates = new string[] { "preview.jpg", "preview.png", "preview.gif" };
            foreach (string c in candidates)
            {
                string p = Path.Combine(dir, c);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private string GetWindowsNativeWallpaperPath()
        {
            try
            {
                string wallpaperPath = new string('\0', 500);
                SystemParametersInfo(SPI_GETDESKWALLPAPER, wallpaperPath.Length, wallpaperPath, 0);
                wallpaperPath = wallpaperPath.Substring(0, wallpaperPath.IndexOf('\0'));

                if (File.Exists(wallpaperPath)) return wallpaperPath;

                string transcoded = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Themes\TranscodedWallpaper");
                if (File.Exists(transcoded)) return transcoded;
            }
            catch { }

            return null;
        }

        private string GetSteamWorkshopPath(string wpPath)
        {
            if (string.IsNullOrEmpty(wpPath)) return null;

            string steamapps = Path.GetFullPath(Path.Combine(wpPath, @"..\.."));
            string workshopPath = Path.Combine(steamapps, @"workshop\content\431960");
            if (Directory.Exists(workshopPath)) return workshopPath;

            return null;
        }

        private string GetWallpaperEnginePath()
        {
            if (!string.IsNullOrEmpty(_customWallpaperEnginePath) && Directory.Exists(_customWallpaperEnginePath))
            {
                return _customWallpaperEnginePath;
            }

            string[] defaultPaths = new string[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\wallpaper_engine",
                @"C:\Program Files\Steam\steamapps\common\wallpaper_engine",
                @"D:\SteamLibrary\steamapps\common\wallpaper_engine",
                @"E:\SteamLibrary\steamapps\common\wallpaper_engine"
            };

            foreach (string p in defaultPaths)
            {
                if (Directory.Exists(p)) return p;
            }

            return null;
        }
    }
}
