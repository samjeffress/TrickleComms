powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe %~dp0UninstallEndpoints.ps1
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe %~dp0SetupInfrastructure.ps1
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe %~dp0InstallEndpoints.ps1
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe %~dp0\build_output\SmsWeb.deploy.cmd /Y