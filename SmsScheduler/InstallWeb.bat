powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe . '%~dp0Installer.ps1'; SetupInfrastructure
%~dp0\build_output\SmsWeb.deploy.cmd /Y