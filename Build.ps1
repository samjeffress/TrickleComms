$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

$clean = $msbuild + " .\SmsScheduler\SmsScheduler.sln /p:Configuration=Release /t:Clean"
$build = $msbuild + " .\SmsScheduler\SmsScheduler.sln /p:Configuration=Release /t:Build"

Invoke-Expression $clean
Invoke-Expression $build