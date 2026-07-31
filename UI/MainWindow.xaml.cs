using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using WinColorSync.Adapters;
using WinColorSync.Core;

namespace WinColorSync.UI
{
    public partial class MainWindow : Window
    {
        private WallpaperEngineWatcher _watcher;
        private NotifyIcon _notifyIcon;
        private ColorPalette _currentPalette;
        private string _configPath;

        public MainWindow()
        {
            InitializeComponent();
            InitializeConfigPath();
            InitializeTrayIcon();
            LoadSettings();

            _watcher = new WallpaperEngineWatcher(TxtWpPath.Text);
            _watcher.WallpaperChanged += OnWallpaperChanged;
            _watcher.Start();
        }

        private void InitializeConfigPath()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dir = Path.Combine(userProfile, ".wincolorsync");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _configPath = Path.Combine(dir, "config.json");
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "WinColorSync - Wallpaper Engine Synchronizer",
                Visible = true
            };

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("⚡ Sync Now", (s, e) => PerformSync());
            contextMenu.MenuItems.Add("⚙️ Settings / Dashboard", (s, e) => ShowDashboard());
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("❌ Exit", (s, e) => ExitApp());

            _notifyIcon.ContextMenu = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowDashboard();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string content = File.ReadAllText(_configPath);
                    if (content.Contains("\"customPath\":"))
                    {
                        int start = content.IndexOf("\"customPath\":") + 13;
                        int firstQuote = content.IndexOf('"', start) + 1;
                        int endQuote = content.IndexOf('"', firstQuote);
                        if (firstQuote > 0 && endQuote > firstQuote)
                        {
                            TxtWpPath.Text = content.Substring(firstQuote, endQuote - firstQuote);
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                string json = string.Format(@"{{
    ""syncSystemAccent"": {0},
    ""autoLightDark"": {1},
    ""tintTitlebars"": {2},
    ""syncTerminal"": {3},
    ""syncVSCode"": {4},
    ""syncFilePilot"": {5},
    ""syncRainmeter"": {6},
    ""exportFiles"": {7},
    ""customPath"": ""{8}""
}}",
                    ChkSystemAccent.IsChecked == true ? "true" : "false",
                    ChkAutoLightDark.IsChecked == true ? "true" : "false",
                    ChkTitlebars.IsChecked == true ? "true" : "false",
                    ChkTerminal.IsChecked == true ? "true" : "false",
                    ChkVSCode.IsChecked == true ? "true" : "false",
                    ChkFilePilot.IsChecked == true ? "true" : "false",
                    ChkRainmeter.IsChecked == true ? "true" : "false",
                    ChkExportFiles.IsChecked == true ? "true" : "false",
                    TxtWpPath.Text.Replace("\\", "\\\\"));

                File.WriteAllText(_configPath, json);
                TxtStatus.Text = "Status: Settings saved successfully!";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Status: Error saving settings: " + ex.Message;
            }
        }

        private void OnWallpaperChanged(object sender, Bitmap wallpaperBitmap)
        {
            Dispatcher.Invoke((Action)delegate
            {
                if (wallpaperBitmap != null)
                {
                    _currentPalette = ColorExtractor.ExtractPalette(wallpaperBitmap);
                    UpdateUiPalette(_currentPalette);
                    ApplySelectedAdapters(_currentPalette);
                }
            });
        }

        private void UpdateUiPalette(ColorPalette palette)
        {
            if (palette == null) return;

            TxtPrimaryHex.Text = palette.PrimaryAccentHex;
            CardPrimary.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(palette.PrimaryAccent.R, palette.PrimaryAccent.G, palette.PrimaryAccent.B));

            TxtSecondaryHex.Text = palette.SecondaryAccentHex;
            CardSecondary.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(palette.SecondaryAccent.R, palette.SecondaryAccent.G, palette.SecondaryAccent.B));

            TxtSurfaceHex.Text = palette.SurfaceAccentHex;
            CardSurface.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(palette.SurfaceAccent.R, palette.SurfaceAccent.G, palette.SurfaceAccent.B));

            TxtBackgroundHex.Text = palette.DarkBackgroundHex;
            CardBackground.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(palette.DarkBackground.R, palette.DarkBackground.G, palette.DarkBackground.B));

            TxtStatus.Text = string.Format("Status: Last synced at {0:HH:mm:ss}", DateTime.Now);
        }

        private void ApplySelectedAdapters(ColorPalette palette)
        {
            if (palette == null) return;

            if (ChkSystemAccent.IsChecked == true)
            {
                WindowsThemeEngine.ApplyPaletteToWindows(palette, ChkTitlebars.IsChecked == true, ChkAutoLightDark.IsChecked == true);
            }

            if (ChkTerminal.IsChecked == true)
            {
                TerminalAdapter.ApplyToTerminal(palette);
            }

            if (ChkVSCode.IsChecked == true)
            {
                VSCodeAdapter.ApplyToVSCode(palette);
            }

            if (ChkFilePilot.IsChecked == true)
            {
                FilePilotAdapter.ApplyToFilePilot(palette);
            }

            if (ChkRainmeter.IsChecked == true)
            {
                RainmeterAdapter.ApplyToRainmeter(palette);
            }

            if (ChkExportFiles.IsChecked == true)
            {
                ThemeExporter.ExportThemeFiles(palette);
            }
        }

        private void PerformSync()
        {
            Bitmap bmp = _watcher.CheckCurrentWallpaper();
            if (bmp != null)
            {
                _currentPalette = ColorExtractor.ExtractPalette(bmp);
                UpdateUiPalette(_currentPalette);
                ApplySelectedAdapters(_currentPalette);
            }
            else
            {
                _currentPalette = ColorExtractor.GetDefaultPalette();
                UpdateUiPalette(_currentPalette);
                ApplySelectedAdapters(_currentPalette);
            }
        }

        private void BtnSyncNow_Click(object sender, RoutedEventArgs e)
        {
            PerformSync();
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            if (_watcher != null)
            {
                _watcher.Stop();
            }
            _watcher = new WallpaperEngineWatcher(TxtWpPath.Text);
            _watcher.WallpaperChanged += OnWallpaperChanged;
            _watcher.Start();
            PerformSync();
        }

        private void BtnBrowsePath_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Wallpaper Engine directory";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TxtWpPath.Text = dlg.SelectedPath;
                }
            }
        }

        private void BtnMinimizeTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            _notifyIcon.ShowBalloonTip(2000, "WinColorSync", "Running in system tray", ToolTipIcon.Info);
        }

        private void ShowDashboard()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApp()
        {
            if (_watcher != null) _watcher.Stop();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            _notifyIcon.ShowBalloonTip(2000, "WinColorSync", "Minimized to tray. Right-click to exit.", ToolTipIcon.Info);
        }
    }
}
