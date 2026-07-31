Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "powershell -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File ""E:\Win Color\SyncWallpaperColors.ps1""", 0, False
