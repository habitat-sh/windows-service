@echo off
powershell.exe -NoProfile -ExecutionPolicy bypass -NoLogo -Command ". '%~dp0habitat.ps1';Uninstall-HabService" %*
