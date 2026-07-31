# WinColorSync - Lightweight Silent Background Sync Script for File Pilot & Antigravity
Add-Type -AssemblyName System.Drawing

$lastWallpaperKey = ""

function Get-WallpaperEnginePath {
    $paths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\wallpaper_engine",
        "C:\Program Files\Steam\steamapps\common\wallpaper_engine",
        "D:\SteamLibrary\steamapps\common\wallpaper_engine",
        "E:\SteamLibrary\steamapps\common\wallpaper_engine",
        "F:\SteamLibrary\steamapps\common\wallpaper_engine"
    )

    try {
        $regPath = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name 'SteamPath' -ErrorAction SilentlyContinue).SteamPath
        if ($regPath) {
            $regPath = $regPath.Replace('/', '\')
            $paths += Join-Path $regPath 'steamapps\common\wallpaper_engine'
        }
    } catch {}

    foreach ($p in $paths) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

function Get-ActiveWallpaperImage {
    $basePath = Get-WallpaperEnginePath
    if (-not $basePath) { return $null }

    $configPath = Join-Path $basePath "config.json"
    if (Test-Path $configPath) {
        try {
            $json = Get-Content $configPath -Raw -ErrorAction SilentlyContinue
            if ($json -match '"selectedwallpapers"\s*:\s*\{[^}]*"file"\s*:\s*"([^"]+)"') {
                $file = $matches[1].Replace('/', '\')
                $dir = Split-Path $file -Parent
                if (Test-Path $dir) {
                    $preview = Get-ChildItem -Path $dir -Include "preview.jpg","preview.png","preview.gif" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($preview) { return @{ Image = $preview.FullName; RawFile = $file } }
                }
            }
        } catch {}
    }

    $steamapps = [System.IO.Path]::GetFullPath("$basePath\..\..")
    $workshopDir = Join-Path $steamapps "workshop\content\431960"
    if (Test-Path $workshopDir) {
        $latest = Get-ChildItem -Path $workshopDir -Include "preview.jpg","preview.png" -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latest) { return @{ Image = $latest.FullName; RawFile = $latest.FullName } }
    }

    $transcoded = "$env:APPDATA\Microsoft\Windows\Themes\TranscodedWallpaper"
    if (Test-Path $transcoded) { return @{ Image = $transcoded; RawFile = $transcoded } }

    return $null
}

function Extract-ColorPalette($wpData) {
    $imgPath = $wpData.Image
    $rawFile = $wpData.RawFile

    # Catppuccin Mocha Official Palette
    if ($rawFile -like "*catppuccin*" -or $rawFile -like "*mocha*" -or $imgPath -like "*catppuccin*" -or $imgPath -like "*mocha*") {
        return @{
            BgHex = "1E1E2E"        # Base
            SurfaceHex = "181825"   # Mantle
            AccentHex = "B4BEFE"    # Lavender
            SecondaryHex = "A6ADC8" # Subtext
            BorderHex = "313244"    # Surface0
            TextHex = "CDD6F4"      # Text
        }
    }

    if (-not (Test-Path $imgPath)) { return $null }

    try {
        $img = [System.Drawing.Image]::FromFile($imgPath)
        $bmp = New-Object System.Drawing.Bitmap($img, 100, 100)
        $img.Dispose()

        $rTot = 0; $gTot = 0; $bTot = 0; $count = 0
        $maxSat = 0; $accentR = 180; $accentG = 190; $accentB = 254

        for ($x = 0; $x -lt 100; $x += 2) {
            for ($y = 0; $y -lt 100; $y += 2) {
                $pixel = $bmp.GetPixel($x, $y)
                $rTot += $pixel.R; $gTot += $pixel.G; $bTot += $pixel.B; $count++

                $max = [Math]::Max($pixel.R, [Math]::Max($pixel.G, $pixel.B))
                $min = [Math]::Min($pixel.R, [Math]::Min($pixel.G, $pixel.B))
                $sat = if ($max -gt 0) { ($max - $min) / $max } else { 0 }

                if ($sat -gt $maxSat -and ($pixel.B -gt $pixel.G -or $pixel.R -gt $pixel.G -or $sat -gt 0.35)) {
                    $maxSat = $sat
                    $accentR = $pixel.R; $accentG = $pixel.G; $accentB = $pixel.B
                }
            }
        }
        $bmp.Dispose()

        if ($count -eq 0) { return $null }

        $avgR = [int]($rTot / $count)
        $avgG = [int]($gTot / $count)
        $avgB = [int]($bTot / $count)

        $bgR = [Math]::Min(30, [int]($avgR * 0.22))
        $bgG = [Math]::Min(30, [int]($avgG * 0.22))
        $bgB = [Math]::Min(46, [int]($avgB * 0.30 + 10))

        $surfaceR = [Math]::Min(40, [int]($bgR + 10))
        $surfaceG = [Math]::Min(40, [int]($bgG + 10))
        $surfaceB = [Math]::Min(55, [int]($bgB + 12))

        return @{
            BgHex = "{0:X2}{1:X2}{2:X2}" -f $bgR, $bgG, $bgB
            SurfaceHex = "{0:X2}{1:X2}{2:X2}" -f $surfaceR, $surfaceG, $surfaceB
            AccentHex = "{0:X2}{1:X2}{2:X2}" -f $accentR, $accentG, $accentB
            SecondaryHex = "A6ADC8"
            BorderHex = "{0:X2}{1:X2}{2:X2}" -f $accentR, $accentG, $accentB
            TextHex = "CDD6F4"
        }
    } catch {
        return $null
    }
}

function Update-FilePilot($palette) {
    $fpConfig = "$env:APPDATA\Voidstar\FilePilot\FPilot-Config.json"
    if (-not (Test-Path $fpConfig)) { return }

    try {
        $content = Get-Content $fpConfig -Raw -ErrorAction SilentlyContinue
        if ($content) {
            $bg = $palette.BgHex
            $surface = $palette.SurfaceHex
            $accent = $palette.AccentHex
            $secondary = $palette.SecondaryHex
            $border = $palette.BorderHex
            $text = $palette.TextHex

            $map = @{
                "Clear" = $bg; "Caption" = $bg; "Background" = $bg; "AlternatingRow" = $bg
                "Surface" = $surface; "Inner" = $surface
                "Border" = $border; "Outline" = $border; "Separator" = $border; "SurfaceSeparator" = $border
                "IconTint" = $accent; "Group" = $accent; "Progress" = $accent; "Selection" = $accent
                "RectSelection" = $accent; "Match" = $accent
                "Foreground" = $text; "File" = $text; "Folder" = $text; "Text" = $text
                "Secondary" = $secondary; "Hover" = $border
            }

            foreach ($key in $map.Keys) {
                $color = $map[$key]
                $content = $content -replace "(?<=`"$key`":\s*`")[0-9A-Fa-f]{6}(?=`")", $color
            }

            Set-Content -Path $fpConfig -Value $content -ErrorAction SilentlyContinue

            $procs = Get-Process -Name "FPilot", "FilePilot" -ErrorAction SilentlyContinue
            if ($procs) {
                foreach ($p in $procs) {
                    $exePath = $p.Path
                    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
                    if ($exePath -and (Test-Path $exePath)) {
                        Start-Process -FilePath $exePath -ErrorAction SilentlyContinue
                    }
                }
            }
        }
    } catch {}
}

function Update-Antigravity($palette) {
    try {
        $exportDir = "$env:USERPROFILE\.wincolorsync"
        if (-not (Test-Path $exportDir)) { New-Item -Path $exportDir -ItemType Directory -Force | Out-Null }

        $css = ":root { `n  --primary-accent: #$($palette.AccentHex); `n  --dark-background: #$($palette.BgHex); `n  --surface-accent: #$($palette.SurfaceHex); `n  --contrast-text: #$($palette.TextHex); `n}"
        Set-Content -Path "$exportDir\theme.css" -Value $css -ErrorAction SilentlyContinue

        $agSettings = "$env:APPDATA\Antigravity\User\settings.json"
        if (Test-Path $agSettings) {
            $content = Get-Content $agSettings -Raw -ErrorAction SilentlyContinue
            $custom = "`"workbench.colorCustomizations`": { `"activityBar.background`": `"#$($palette.BgHex)`", `"activityBar.activeBorder`": `"#$($palette.AccentHex)`", `"statusBar.background`": `"#$($palette.AccentHex)`", `"sideBar.background`": `"#$($palette.BgHex)`", `"editor.background`": `"#$($palette.BgHex)`" }"
            if ($content -match '"workbench\.colorCustomizations"') {
                $content = $content -replace '"workbench\.colorCustomizations"\s*:\s*\{[^}]+\}', $custom
            } else {
                $idx = $content.LastIndexOf('}')
                if ($idx -ge 0) { $content = $content.Insert($idx, ",`n" + $custom + "`n") }
            }
            Set-Content -Path $agSettings -Value $content -ErrorAction SilentlyContinue
        }
    } catch {}
}

# Main loop
while ($true) {
    $wpData = Get-ActiveWallpaperImage
    if ($wpData -and $wpData.Image) {
        $writeTime = (Get-Item $wpData.Image).LastWriteTime.Ticks
        $key = "$($wpData.Image)_$writeTime"

        if ($key -ne $lastWallpaperKey) {
            $lastWallpaperKey = $key
            $palette = Extract-ColorPalette $wpData
            if ($palette) {
                Update-FilePilot $palette
                Update-Antigravity $palette
            }
        }
    }
    Start-Sleep -Seconds 3
}
