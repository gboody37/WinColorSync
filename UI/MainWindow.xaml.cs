using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinColorSync.Adapters;
using WinColorSync.Core;

namespace WinColorSync.UI
{
    public partial class MainWindow : Window
    {
        private WallpaperEngineWatcher _watcher;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private ColorPalette _currentPalette;
        private string _configPath;
        private bool _isUpdatingUi = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeConfigPath();
            InitializeTrayIcon();
            LoadSettings();

            if (string.IsNullOrEmpty(TxtWpPath.Text))
            {
                TxtWpPath.Text = WallpaperEngineWatcher.AutoDetectWallpaperEnginePath();
            }

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
            System.Drawing.Icon icon = SystemIcons.Application;
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppIcon.png");
                if (File.Exists(iconPath))
                {
                    using (Bitmap bmp = new Bitmap(iconPath))
                    {
                        icon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch { }

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = icon,
                Text = "WinColorSync - Wallpaper Engine Synchronizer",
                Visible = true
            };

            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.Add("⚡ Sync Now", (s, e) => PerformSync());
            contextMenu.MenuItems.Add("📸 Capture Screen Colors", (s, e) => PerformScreenCapture());
            contextMenu.MenuItems.Add("🔄 Reset Windows Defaults", (s, e) => ResetWindowsDefaults());
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

            _isUpdatingUi = true;

            TxtPrimaryHex.Text = palette.PrimaryAccentHex;
            SwatchPrimary.Background = GetMediaBrush(palette.PrimaryAccent);

            TxtSecondaryHex.Text = palette.SecondaryAccentHex;
            SwatchSecondary.Background = GetMediaBrush(palette.SecondaryAccent);

            TxtBorderHex.Text = palette.WindowBorderHex;
            SwatchBorder.Background = GetMediaBrush(palette.WindowBorder);

            TxtTextHex.Text = palette.ContrastTextHex;
            SwatchText.Background = GetMediaBrush(palette.ContrastText);

            TxtSurfaceHex.Text = palette.SurfaceAccentHex;
            SwatchSurface.Background = GetMediaBrush(palette.SurfaceAccent);

            TxtBackgroundHex.Text = palette.DarkBackgroundHex;
            SwatchBackground.Background = GetMediaBrush(palette.DarkBackground);

            TxtStatus.Text = string.Format("Status: Last synced at {0:HH:mm:ss}", DateTime.Now);

            _isUpdatingUi = false;
        }

        private System.Windows.Media.SolidColorBrush GetMediaBrush(Color c)
        {
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));
        }

        private ColorPalette BuildPaletteFromUi()
        {
            Color primary = ColorPalette.HexToColor(TxtPrimaryHex.Text, Color.FromArgb(0, 120, 215));
            Color secondary = ColorPalette.HexToColor(TxtSecondaryHex.Text, Color.FromArgb(0, 90, 160));
            Color border = ColorPalette.HexToColor(TxtBorderHex.Text, primary);
            Color text = ColorPalette.HexToColor(TxtTextHex.Text, Color.White);
            Color surface = ColorPalette.HexToColor(TxtSurfaceHex.Text, Color.FromArgb(32, 40, 50));
            Color background = ColorPalette.HexToColor(TxtBackgroundHex.Text, Color.FromArgb(24, 24, 28));

            return new ColorPalette
            {
                PrimaryAccent = primary,
                SecondaryAccent = secondary,
                WindowBorder = border,
                ContrastText = text,
                SurfaceAccent = surface,
                DarkBackground = background,
                LightBackground = Color.FromArgb(245, 245, 248),
                IsDarkThemeRecommended = ColorExtractor.GetBrightness(background) < 0.6
            };
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
                PerformScreenCapture();
            }
        }

        private void PerformScreenCapture()
        {
            Hide();
            System.Threading.Thread.Sleep(300);
            Bitmap screenBmp = _watcher.CapturePrimaryScreen();
            Show();
            if (screenBmp != null)
            {
                _currentPalette = ColorExtractor.ExtractPalette(screenBmp);
                UpdateUiPalette(_currentPalette);
                ApplySelectedAdapters(_currentPalette);
                TxtStatus.Text = string.Format("Status: Captured screen colors at {0:HH:mm:ss}", DateTime.Now);
            }
        }

        private void ResetWindowsDefaults()
        {
            WindowsThemeEngine.ResetWindowsDefaultColors();
            _currentPalette = ColorExtractor.GetDefaultPalette();
            UpdateUiPalette(_currentPalette);
            TxtStatus.Text = "Status: Restored native Windows default accent colors!";
        }

        private void Swatch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            using (System.Windows.Forms.ColorDialog dlg = new System.Windows.Forms.ColorDialog())
            {
                dlg.FullOpen = true;

                Color currentColor = Color.White;
                if (sender == SwatchPrimary) currentColor = ColorPalette.HexToColor(TxtPrimaryHex.Text, Color.FromArgb(0, 120, 215));
                else if (sender == SwatchSecondary) currentColor = ColorPalette.HexToColor(TxtSecondaryHex.Text, Color.FromArgb(0, 90, 160));
                else if (sender == SwatchBorder) currentColor = ColorPalette.HexToColor(TxtBorderHex.Text, Color.FromArgb(0, 120, 215));
                else if (sender == SwatchText) currentColor = ColorPalette.HexToColor(TxtTextHex.Text, Color.White);
                else if (sender == SwatchSurface) currentColor = ColorPalette.HexToColor(TxtSurfaceHex.Text, Color.FromArgb(32, 40, 50));
                else if (sender == SwatchBackground) currentColor = ColorPalette.HexToColor(TxtBackgroundHex.Text, Color.FromArgb(24, 24, 28));

                dlg.Color = currentColor;

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string hex = ColorPalette.ColorToHex(dlg.Color);

                    if (sender == SwatchPrimary) TxtPrimaryHex.Text = hex;
                    else if (sender == SwatchSecondary) TxtSecondaryHex.Text = hex;
                    else if (sender == SwatchBorder) TxtBorderHex.Text = hex;
                    else if (sender == SwatchText) TxtTextHex.Text = hex;
                    else if (sender == SwatchSurface) TxtSurfaceHex.Text = hex;
                    else if (sender == SwatchBackground) TxtBackgroundHex.Text = hex;

                    _currentPalette = BuildPaletteFromUi();
                    ApplySelectedAdapters(_currentPalette);
                    TxtStatus.Text = string.Format("Status: Color selected from wheel at {0:HH:mm:ss}", DateTime.Now);
                }
            }
        }

        private void TxtColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            try
            {
                if (sender == TxtPrimaryHex) SwatchPrimary.Background = GetMediaBrush(ColorPalette.HexToColor(TxtPrimaryHex.Text, Color.Transparent));
                else if (sender == TxtSecondaryHex) SwatchSecondary.Background = GetMediaBrush(ColorPalette.HexToColor(TxtSecondaryHex.Text, Color.Transparent));
                else if (sender == TxtBorderHex) SwatchBorder.Background = GetMediaBrush(ColorPalette.HexToColor(TxtBorderHex.Text, Color.Transparent));
                else if (sender == TxtTextHex) SwatchText.Background = GetMediaBrush(ColorPalette.HexToColor(TxtTextHex.Text, Color.Transparent));
                else if (sender == TxtSurfaceHex) SwatchSurface.Background = GetMediaBrush(ColorPalette.HexToColor(TxtSurfaceHex.Text, Color.Transparent));
                else if (sender == TxtBackgroundHex) SwatchBackground.Background = GetMediaBrush(ColorPalette.HexToColor(TxtBackgroundHex.Text, Color.Transparent));
            }
            catch { }
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            ResetWindowsDefaults();
        }

        private void BtnApplyCustomColors_Click(object sender, RoutedEventArgs e)
        {
            _currentPalette = BuildPaletteFromUi();
            ApplySelectedAdapters(_currentPalette);
            TxtStatus.Text = string.Format("Status: Custom colors applied at {0:HH:mm:ss}", DateTime.Now);
        }

        private void BtnSyncNow_Click(object sender, RoutedEventArgs e)
        {
            PerformSync();
        }

        private void BtnCaptureScreen_Click(object sender, RoutedEventArgs e)
        {
            PerformScreenCapture();
        }

        private void BtnAutoDetect_Click(object sender, RoutedEventArgs e)
        {
            string detected = WallpaperEngineWatcher.AutoDetectWallpaperEnginePath();
            if (!string.IsNullOrEmpty(detected))
            {
                TxtWpPath.Text = detected;
                TxtStatus.Text = "Status: Auto-detected Wallpaper Engine at " + detected;
                SaveSettings();
                BtnSaveSettings_Click(sender, e);
            }
            else
            {
                TxtStatus.Text = "Status: Wallpaper Engine not found automatically. Use Browse button.";
            }
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
            using (System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog())
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
            _notifyIcon.ShowBalloonTip(2000, "WinColorSync", "Running in system tray", System.Windows.Forms.ToolTipIcon.Info);
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
            _notifyIcon.ShowBalloonTip(2000, "WinColorSync", "Minimized to tray. Right-click to exit.", System.Windows.Forms.ToolTipIcon.Info);
        }
    }
}
