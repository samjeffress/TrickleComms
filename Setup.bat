powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe .\UninstallEndpoints.ps1
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe .\Build.ps1
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe .\SetupInfrastructure.ps1
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe .\InstallEndpoints.ps1
powershell.exe Set-ExecutionPolicy remotesigned
powershell.exe .\InstallWeb.ps1