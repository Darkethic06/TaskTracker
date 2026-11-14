# Builds TaskTrackerSetup-x.y.z.exe into .\installer\output
# Requirements: .NET 8 SDK, Inno Setup 6
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "1/2  Publishing self-contained release build..." -ForegroundColor Cyan
dotnet publish "$root\TaskTracker.App\TaskTracker.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=false -p:DebugType=none
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "2/2  Compiling installer..." -ForegroundColor Cyan
$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 not found. Install it with: winget install JRSoftware.InnoSetup" }

& $iscc "$root\installer\TaskTracker.iss"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compile failed" }

Write-Host "`nDone! Installer:" -ForegroundColor Green
Get-ChildItem "$root\installer\output\*.exe" | ForEach-Object { "  $($_.FullName)  ($([math]::Round($_.Length/1MB,1)) MB)" }
