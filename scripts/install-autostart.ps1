# ==============================================================================
# Kursor Windows Autostart & Shortcut Installer (scripts/install-autostart.ps1)
# Registers Kursor to launch automatically when Windows starts
# (HKCU\Software\Microsoft\Windows\CurrentVersion\Run -> Kursor)
# and creates a Desktop shortcut.
# ==============================================================================
param(
    [switch]$SkipDesktopShortcut
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$VbsPath = Join-Path $PSScriptRoot "start-kursor-hidden.vbs"
$DistExe = Join-Path $RepoRoot "dist\kursor-desktop\HeyClicky.exe"
$IconPath = if (Test-Path $DistExe) { "$DistExe,0" } else { "shell32.dll,24" }

if (-not (Test-Path $VbsPath)) {
    throw "Missing autostart launcher script: $VbsPath"
}

Write-Host "==> Registering Kursor in Windows Run Registry..." -ForegroundColor Cyan
$RunKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$WscriptCmd = "wscript.exe //B //Nologo `"$VbsPath`""
Set-ItemProperty -Path $RunKeyPath -Name "Kursor" -Value $WscriptCmd
Write-Host "   [+] Registry key added: HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Kursor" -ForegroundColor Green

# Remove any legacy HKCU\...\Run\HeyClicky entry so only one instance launches at login
if (Get-ItemProperty -Path $RunKeyPath -Name "HeyClicky" -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $RunKeyPath -Name "HeyClicky" -Force -ErrorAction SilentlyContinue
    Write-Host "   [-] Removed legacy HKCU\Software\Microsoft\Windows\CurrentVersion\Run\HeyClicky" -ForegroundColor Yellow
}

# Remove any duplicate Startup\Kursor.lnk shortcut if present from older installs
$LegacyStartupShortcut = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::Startup)) "Kursor.lnk"
if (Test-Path $LegacyStartupShortcut) {
    Remove-Item -Path $LegacyStartupShortcut -Force -ErrorAction SilentlyContinue
    Write-Host "   [-] Removed duplicate shortcut: $LegacyStartupShortcut" -ForegroundColor Yellow
}

if (-not $SkipDesktopShortcut) {
    Write-Host "==> Creating Desktop Shortcut..." -ForegroundColor Cyan
    $WshShell = New-Object -ComObject WScript.Shell
    $DesktopFolder = [Environment]::GetFolderPath("Desktop")
    $DesktopShortcutPath = Join-Path $DesktopFolder "Kursor.lnk"
    $DesktopShortcut = $WshShell.CreateShortcut($DesktopShortcutPath)
    $DesktopShortcut.TargetPath = "wscript.exe"
    $DesktopShortcut.Arguments = "//B //Nologo `"$VbsPath`""
    $DesktopShortcut.WorkingDirectory = $RepoRoot
    $DesktopShortcut.Description = "Launch Kursor AI Companion & Backend"
    $DesktopShortcut.IconLocation = $IconPath
    $DesktopShortcut.Save()
    Write-Host "   [+] Desktop shortcut created: $DesktopShortcutPath" -ForegroundColor Green
}

Write-Host "`n[SUCCESS] Kursor is now configured to start automatically when your computer starts!" -ForegroundColor Green
Write-Host "  - Registry -> HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Kursor" -ForegroundColor Gray
Write-Host "  - To disable autostart later, run: .\scripts\uninstall-autostart.ps1" -ForegroundColor Gray
