' ==============================================================================
' Kursor Silent Autostart Wrapper (scripts/start-kursor-hidden.vbs)
' Launches Kursor Backend (hidden) and Kursor Desktop Suite on Windows login
' without flashing a console or PowerShell window.
' ==============================================================================
Option Explicit

Dim fso, scriptDir, repoRoot, psScript, shell, cmd
Set fso = CreateObject("Scripting.FileSystemObject")
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
repoRoot = fso.GetParentFolderName(scriptDir)
psScript = fso.BuildPath(repoRoot, "start-kursor.ps1")

Set shell = CreateObject("WScript.Shell")
shell.CurrentDirectory = repoRoot
cmd = "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & psScript & """ -Hidden"
shell.Run cmd, 0, False
