# ==============================================================================
# Kursor Windows Autostart Uninstaller (scripts/uninstall-autostart.ps1)
# Removes Kursor from Windows Startup Registry and Startup Folder.
# ==============================================================================
$ErrorActionPreference = "Continue"

$RunKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
if (Get-ItemProperty -Path $RunKeyPath -Name "Kursor" -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $RunKeyPath -Name "Kursor" -Force
    Write-Host "[+] Removed 'Kursor' from HKCU\Software\Microsoft\Windows\CurrentVersion\Run" -ForegroundColor Green
} else {
    Write-Host "[*] 'Kursor' was not registered in HKCU Run registry." -ForegroundColor Gray
}

$StartupShortcutPath = Join-Path ([Environment]::GetFolderPath("Startup")) "Kursor.lnk"
if (Test-Path $StartupShortcutPath) {
    Remove-Item -Path $StartupShortcutPath -Force
    Write-Host "[+] Removed Startup shortcut: $StartupShortcutPath" -ForegroundColor Green
} else {
    Write-Host "[*] Startup shortcut not found at $StartupShortcutPath" -ForegroundColor Gray
}

Write-Host "[SUCCESS] Kursor autostart has been disabled." -ForegroundColor Cyan
