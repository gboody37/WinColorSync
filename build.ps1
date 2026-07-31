# Build WinColorSync using native Windows MSBuild.exe
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

Write-Host "🎨 Building WinColorSync via MSBuild..." -ForegroundColor Cyan

& $msbuild WinColorSync.csproj /p:Configuration=Release /v:minimal

if (Test-Path "bin\Release\WinColorSync.exe") {
    Write-Host "✅ Successfully built bin\Release\WinColorSync.exe!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed." -ForegroundColor Red
}
