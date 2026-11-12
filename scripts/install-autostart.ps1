# ==============================================================================
# Kursor Windows Autostart & Shortcut Installer (scripts/install-autostart.ps1)
# Registers Kursor to launch automatically when Windows starts (HKCU\...\Run +
# Startup folder shortcut) and creates a Desktop shortcut.
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

Write-Host "==> Registering Kursor in Windows Startup Apps (HKCU Run Registry)..." -ForegroundColor Cyan
$RunKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$WscriptCmd = "wscript.exe //B //Nologo `"$VbsPath`""
Set-ItemProperty -Path $RunKeyPath -Name "Kursor" -Value $WscriptCmd
Write-Host "   [+] Registry key added: HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Kursor" -ForegroundColor Green

Write-Host "==> Creating Windows Startup Folder Shortcut..." -ForegroundColor Cyan
$StartupFolder = [Environment]::GetFolderPath("Startup")
$StartupShortcutPath = Join-Path $StartupFolder "Kursor.lnk"

$WshShell = New-Object -ComObject WScript.Shell
$StartupShortcut = $WshShell.CreateShortcut($StartupShortcutPath)
$StartupShortcut.TargetPath = "wscript.exe"
$StartupShortcut.Arguments = "//B //Nologo `"$VbsPath`""
$StartupShortcut.WorkingDirectory = $RepoRoot
$StartupShortcut.Description = "Kursor — Self-Hosted AI Screen Companion & Backend"
$StartupShortcut.IconLocation = $IconPath
$StartupShortcut.Save()
Write-Host "   [+] Startup shortcut created: $StartupShortcutPath" -ForegroundColor Green

if (-not $SkipDesktopShortcut) {
    Write-Host "==> Creating Desktop Shortcut..." -ForegroundColor Cyan
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
Write-Host "  - Task Manager -> Startup apps -> 'Kursor' (wscript.exe)" -ForegroundColor Gray
Write-Host "  - To disable autostart later, run: .\scripts\uninstall-autostart.ps1" -ForegroundColor Gray
