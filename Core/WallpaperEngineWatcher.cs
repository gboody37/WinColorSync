using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace WinColorSync.Core
{
    public class WallpaperEngineWatcher
    {
        public event EventHandler<Bitmap> WallpaperChanged;

        private FileSystemWatcher _watcher;
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

            if (Directory.Exists(wpPath))
            {
                try
                {
                    _watcher = new FileSystemWatcher(wpPath)
                    {
                        IncludeSubdirectories = true,
                        Filter = "*.*",
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
                    };

                    _watcher.Changed += OnDirectoryChanged;
                    _watcher.Created += OnDirectoryChanged;
                    _watcher.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WallpaperEngineWatcher] Watcher error: " + ex.Message);
                }
            }

            CheckCurrentWallpaper();
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private void OnDirectoryChanged(object sender, FileSystemEventArgs e)
        {
            CheckCurrentWallpaper();
        }

        public Bitmap CheckCurrentWallpaper()
        {
            try
            {
                string wpImage = FindWallpaperEngineActivePreview();

                if (string.IsNullOrEmpty(wpImage) || !File.Exists(wpImage))
                {
                    wpImage = GetWindowsNativeWallpaperPath();
                }

                if (!string.IsNullOrEmpty(wpImage) && File.Exists(wpImage) && wpImage != _lastWallpaperPath)
                {
                    _lastWallpaperPath = wpImage;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WallpaperEngineWatcher] Error checking wallpaper: " + ex.Message);
            }

            return null;
        }

        private string FindWallpaperEngineActivePreview()
        {
            string basePath = GetWallpaperEnginePath();
            if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath)) return null;

            string projectsDir = Path.Combine(basePath, @"projects\defaultprojects");
            string myProjectsDir = Path.Combine(basePath, @"projects\myprojects");

            string bestPreview = FindLatestPreviewFile(projectsDir);
            if (string.IsNullOrEmpty(bestPreview))
            {
                bestPreview = FindLatestPreviewFile(myProjectsDir);
            }

            return bestPreview;
        }

        private string FindLatestPreviewFile(string dir)
        {
            if (!Directory.Exists(dir)) return null;

            DateTime newest = DateTime.MinValue;
            string newestFile = null;

            foreach (string subDir in Directory.GetDirectories(dir))
            {
                string[] candidates = new string[] { "preview.jpg", "preview.png", "preview.gif" };
                foreach (string c in candidates)
                {
                    string p = Path.Combine(subDir, c);
                    if (File.Exists(p))
                    {
                        DateTime writeTime = File.GetLastWriteTime(p);
                        if (writeTime > newest)
                        {
                            newest = writeTime;
                            newestFile = p;
                        }
                    }
                }
            }

            return newestFile;
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
