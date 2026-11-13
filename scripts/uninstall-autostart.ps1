# ==============================================================================
# Kursor Windows Autostart Uninstaller (scripts/uninstall-autostart.ps1)
# Removes Kursor and legacy HeyClicky entries from Windows Run Registry,
# Startup Folder, and optionally the Desktop shortcut.
# ==============================================================================
param(
    [switch]$RemoveDesktopShortcut
)

$ErrorActionPreference = "Continue"

$RunKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
foreach ($name in @("Kursor", "HeyClicky")) {
    if (Get-ItemProperty -Path $RunKeyPath -Name $name -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $RunKeyPath -Name $name -Force -ErrorAction SilentlyContinue
        Write-Host "[+] Removed '$name' from HKCU\Software\Microsoft\Windows\CurrentVersion\Run" -ForegroundColor Green
    } else {
        Write-Host "[*] '$name' was not registered in HKCU Run registry." -ForegroundColor Gray
    }
}

$StartupShortcutPath = Join-Path ([Environment]::GetFolderPath("Startup")) "Kursor.lnk"
if (Test-Path $StartupShortcutPath) {
    Remove-Item -Path $StartupShortcutPath -Force -ErrorAction SilentlyContinue
    Write-Host "[+] Removed Startup shortcut: $StartupShortcutPath" -ForegroundColor Green
} else {
    Write-Host "[*] Startup shortcut not found at $StartupShortcutPath" -ForegroundColor Gray
}

if ($RemoveDesktopShortcut) {
    $DesktopShortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "Kursor.lnk"
    if (Test-Path $DesktopShortcutPath) {
        Remove-Item -Path $DesktopShortcutPath -Force -ErrorAction SilentlyContinue
        Write-Host "[+] Removed Desktop shortcut: $DesktopShortcutPath" -ForegroundColor Green
    } else {
        Write-Host "[*] Desktop shortcut not found at $DesktopShortcutPath" -ForegroundColor Gray
    }
}

Write-Host "[SUCCESS] Kursor autostart has been disabled." -ForegroundColor Cyan
