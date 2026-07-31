# 🎨 WinColorSync

**WinColorSync** is a native, lightweight Windows application designed to automatically synchronize your **Wallpaper Engine** (or native Windows desktop wallpaper) colors across your entire Windows operating system and third-party desktop applications in real time.

---

## ✨ Features

- **⚡ Real-time Wallpaper Engine Integration:** Automatically detects when Wallpaper Engine changes wallpapers and extracts dominant color palettes instantly.
- **🖼️ Native Windows Wallpaper Fallback:** Works seamlessly even if Wallpaper Engine is paused or not running.
- **🎨 Color Quantization (K-Means):** Extracts 6 balanced colors: Primary Accent, Secondary Accent, Background Dark, Background Light, Surface Accent, and High-Contrast Text.
- **🖥️ System DWM & Registry Theme Sync:** Updates Windows 10/11 system accent color dynamically (`DwmSetColorizationParameters` + Registry) without requiring a system restart.
- **🌓 Automatic Light / Dark Mode Switching:** Adapts system theme mode dynamically based on wallpaper brightness and contrast.
- **🪟 Windows Titlebar & Border Accent Tinting:** Applies matching title bar colors to all open application windows using `DwmSetWindowAttribute`.
- **💻 Windows Terminal Adapter:** Dynamically updates Microsoft Windows Terminal `settings.json` schemes to match active desktop colors.
- **📝 VS Code Adapter:** Automatically injects matching theme colors into Visual Studio Code `workbench.colorCustomizations`.
- **☔ Rainmeter Adapter:** Writes extracted hex codes to Rainmeter `#@#Variables.inc` and refreshes skins (`!RefreshApp`).
- **📤 Generic Exporter (`theme.css` & `theme.json`):** Generates standardized theme files at `%USERPROFILE%\.wincolorsync\` for custom web injectors (Spicetify, Vencord, Stylus).
- **📌 System Tray Background App:** Runs quietly in the Windows notification area with simple right-click controls.

---

## 🛠️ Architecture & Requirements

- **OS:** Windows 10 / Windows 11 (64-bit)
- **Framework:** .NET Framework 4.8 / .NET 8 (WPF + Win32 API)
- **APIs:** Win32 DWM API (`dwmapi.dll`), User32 (`user32.dll`), Registry (`advapi32.dll`), System.Drawing / GDI+.

---

## 🚀 How to Build & Run

### Option 1: Quick PowerShell Build (Native Windows - No Visual Studio Required)
Open PowerShell in the project directory and run:
```powershell
.\build.ps1
```
This will compile `WinColorSync.exe` using native Windows C# compiler (`csc.exe`).

### Option 2: Building via Visual Studio / MSBuild
1. Open `WinColorSync.csproj` in Visual Studio 2022 or later.
2. Select **Release | x64** or **Release | Any CPU**.
3. Press **Build Solution** (`Ctrl+Shift+B`).
4. Run `bin\Release\WinColorSync.exe`.

---

## 📖 Usage

1. Launch `WinColorSync.exe`.
2. The app will start minimized in your **System Tray** (near the clock).
3. Right-click the tray icon to:
   - **Sync Now:** Force an immediate color sync from the active wallpaper.
   - **Settings:** Enable or disable specific app adapters (Terminal, VS Code, Titlebars).
   - **Set Wallpaper Engine Directory:** Configure custom Steam library paths.
   - **Exit:** Close the application.

---

## 📄 License

MIT License. Free and open source!
