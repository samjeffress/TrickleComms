powershell.exe Set-ExecutionPolicy remotesigned
SET batDirectory = %~dp0
powershell.exe . '%~dp0Installer.ps1'; UnitTests