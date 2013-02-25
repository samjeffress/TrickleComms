function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$path = Get-ScriptDirectory Build.ps1

$clean = $msbuild + " " + $path + "\SmsScheduler\SmsScheduler.sln /p:Configuration=Release /t:Clean"
$build = $msbuild + " " + $path + "\SmsScheduler\SmsScheduler.sln /p:Configuration=Release /t:Build"

Invoke-Expression $clean
Invoke-Expression $build