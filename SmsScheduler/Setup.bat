powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe . %~dp0SmsScheduler\Installer.ps1; UninstallEndpoints
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe . %~dp0SmsScheduler\Installer.ps1; Build
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe . %~dp0SmsScheduler\Installer.ps1; UnitTests
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe . %~dp0SmsScheduler\Installer.ps1; SetupInfrastructure
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe . %~dp0SmsScheduler\Installer.ps1; InstallEndpoints
powershell.exe Set-ExecutionPolicy remotesigned
%~dp0\build_output\SmsWeb.deploy.cmd /Y