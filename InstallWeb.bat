powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe %~dp0SetupInfrastructure.ps1
%~dp0\build_output\SmsWeb.deploy.cmd /Y