using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinColorSync.Core
{
    public static class WindowsThemeEngine
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DWMCOLORIZATIONPARAMS
        {
            public uint ColorizationColor;
            public uint ColorizationAfterglow;
            public uint ColorizationColorBalance;
            public uint ColorizationAfterglowBalance;
            public uint ColorizationBlurBalance;
            public uint ColorizationGlassReflectionIntensity;
            public uint ColorizationOpaqueBlend;
        }

        [DllImport("dwmapi.dll", EntryPoint = "#127", PreserveSig = false)]
        private static extern void DwmGetColorizationParameters(out DWMCOLORIZATIONPARAMS parameters);

        [DllImport("dwmapi.dll", EntryPoint = "#131", PreserveSig = false)]
        private static extern void DwmSetColorizationParameters(ref DWMCOLORIZATIONPARAMS parameters, uint uUnknown);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            UIntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWA_BORDER_COLOR = 34;

        public static void ApplyPaletteToWindows(ColorPalette palette, bool updateTitlebars, bool autoLightDarkMode)
        {
            if (palette == null) return;

            Color accent = palette.PrimaryAccent;

            try
            {
                SetRegistryAccentColors(accent);

                uint argb = (uint)((0xFF << 24) | (accent.R << 16) | (accent.G << 8) | accent.B);
                try
                {
                    DWMCOLORIZATIONPARAMS paramsDwm;
                    DwmGetColorizationParameters(out paramsDwm);
                    paramsDwm.ColorizationColor = argb;
                    paramsDwm.ColorizationColorBalance = 100;
                    DwmSetColorizationParameters(ref paramsDwm, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WindowsThemeEngine] DwmSetColorizationParameters warning: " + ex.Message);
                }

                if (autoLightDarkMode)
                {
                    SetSystemThemeMode(palette.IsDarkThemeRecommended);
                }

                UIntPtr result;
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "ImmersiveColorSet", SMTO_ABORTIFHUNG, 100, out result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WindowsThemeEngine] Error applying theme: " + ex.Message);
            }
        }

        private static void SetRegistryAccentColors(Color accent)
        {
            try
            {
                byte[] accentPalette = new byte[32];
                accentPalette[0] = accent.R;
                accentPalette[1] = accent.G;
                accentPalette[2] = accent.B;
                accentPalette[3] = 0xFF;

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent"))
                {
                    if (key != null)
                    {
                        key.SetValue("AccentColorMenu", (int)((0xFF << 24) | (accent.B << 16) | (accent.G << 8) | accent.R), RegistryValueKind.DWord);
                        key.SetValue("AccentPalette", accentPalette, RegistryValueKind.Binary);
                    }
                }

                using (RegistryKey dwmKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\Dwm"))
                {
                    if (dwmKey != null)
                    {
                        dwmKey.SetValue("ColorizationColor", (int)((0xFF << 24) | (accent.R << 16) | (accent.G << 8) | accent.B), RegistryValueKind.DWord);
                        dwmKey.SetValue("AccentColor", (int)((0xFF << 24) | (accent.B << 16) | (accent.G << 8) | accent.R), RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WindowsThemeEngine] Registry update error: " + ex.Message);
            }
        }

        public static void SetSystemThemeMode(bool isDark)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        int value = isDark ? 0 : 1;
                        key.SetValue("AppsUseLightTheme", value, RegistryValueKind.DWord);
                        key.SetValue("SystemUsesLightTheme", value, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WindowsThemeEngine] Theme mode registry error: " + ex.Message);
            }
        }

        public static void ApplyTitlebarColorToWindow(IntPtr hWnd, Color titleColor, Color textColor)
        {
            if (hWnd == IntPtr.Zero) return;

            try
            {
                int colorBGR = (titleColor.B << 16) | (titleColor.G << 8) | titleColor.R;
                DwmSetWindowAttribute(hWnd, DWMWA_CAPTION_COLOR, ref colorBGR, sizeof(int));

                int textBGR = (textColor.B << 16) | (textColor.G << 8) | textColor.R;
                DwmSetWindowAttribute(hWnd, DWMWA_TEXT_COLOR, ref textBGR, sizeof(int));
            }
            catch { }
        }
    }
}
