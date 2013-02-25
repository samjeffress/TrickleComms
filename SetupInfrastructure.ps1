function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$path = Get-ScriptDirectory SetupInfrastructure.ps1

Import-Module Join-Path $path -childpath "\build_output\SmsCoordinator\NServiceBus.Core.dll"
Install-Dtc
Install-Msmq
Install-RavenDB
Install-PerformanceCounters