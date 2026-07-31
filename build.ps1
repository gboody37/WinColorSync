# Automatically set working directory to script folder
Set-Location -Path $PSScriptRoot

$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

Write-Host "🎨 Building WinColorSync via MSBuild..." -ForegroundColor Cyan

& $msbuild WinColorSync.csproj /p:Configuration=Release /v:minimal

if (Test-Path "bin\Release\WinColorSync.exe") {
    Write-Host "`n✅ Successfully built bin\Release\WinColorSync.exe!" -ForegroundColor Green
    Write-Host "🚀 Run the application using:" -ForegroundColor Yellow
    Write-Host "   .\bin\Release\WinColorSync.exe" -ForegroundColor White
} else {
    Write-Host "`n❌ Build failed." -ForegroundColor Red
}
