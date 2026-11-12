@echo off
title Kursor Multi-Device Setup
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-kursor.ps1" %*
pause
