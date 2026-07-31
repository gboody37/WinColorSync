using System;
using System.Drawing;
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

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();

            _watcher = new WallpaperEngineWatcher();
            _watcher.WallpaperChanged += OnWallpaperChanged;
            _watcher.Start();
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
            contextMenu.MenuItems.Add("🖥️ Open Dashboard", (s, e) => ShowDashboard());
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("❌ Exit", (s, e) => ExitApp());

            _notifyIcon.ContextMenu = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowDashboard();
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
