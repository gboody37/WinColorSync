# Installs WinColorSync background script into Windows Startup
$startupFolder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
$shortcutPath = Join-Path $startupFolder "WinColorSyncBackground.lnk"

$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = "wscript.exe"
$Shortcut.Arguments = """E:\Win Color\StartBackgroundSync.vbs"""
$Shortcut.WorkingDirectory = "E:\Win Color"
$Shortcut.Save()

Write-Host "✅ WinColorSync silent background script added to Windows Startup!" -ForegroundColor Green
Write-Host "Path: $shortcutPath" -ForegroundColor Yellow
