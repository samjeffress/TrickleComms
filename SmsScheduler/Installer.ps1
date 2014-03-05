
function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$installFolder = "c:\SmsServices"
$path = Get-ScriptDirectory Installer.ps1
$build_output = (Get-Item $path).parent.FullName + '\build_output\'
#$go_environment = (get-item env:GO_ENVIRONMENT_NAME).Value

if (Test-Path env:GO_ENVIRONMENT_NAME)
{
    $go_environment = (get-item env:GO_ENVIRONMENT_NAME).Value
}
else
{
    $go_environment = "UAT"
}

function InstallService([string]$DirectoryName, [string]$ServiceDescription)
{
    echo "Installing service $DirectoryName"
    if(Get-Service $DirectoryName -ErrorAction SilentlyContinue)
    {
        echo "Service $DirectoryName exists - stopping"
        Stop-Service $DirectoryName
        echo "Stopped Service"        
        if ([IO.Directory]::Exists($($installFolder + "\" + $DirectoryName))) 
        {
            echo "Removing existing files"
            Remove-Item -Recurse -Force $($installFolder + "\" + $DirectoryName)
        }
        echo "Copying files from $build_output $DirectoryName to $installFolder $DirectoryName"
        Copy-Item $($build_output + $DirectoryName) $($installFolder + "\" + $DirectoryName) -recurse -Force
        echo "Finished copying"
    }else{
        echo "Service does not exist"
        if ([IO.Directory]::Exists($($installFolder + "\" + $DirectoryName))) 
        {
            echo "Removing existing files"
            Remove-Item -Recurse -Force $($installFolder + "\" + $DirectoryName)
        }
        echo "Copying files from $build_output $DirectoryName to $installFolder $DirectoryName"
        Copy-Item $($build_output + $DirectoryName) $($installFolder + "\" + $DirectoryName) -Recurse
        echo "Finished copying"
        echo "Installing service $DirectoryName"
        $nsbHost = Join-Path $installFolder -childpath "\$DirectoryName\NServiceBus.Host.exe"
		$argList = "/install /serviceName:$DirectoryName /displayName:$DirectoryName /description:'$ServiceDescription' NServiceBus.Production"
		RunCommand $nsbHost $argList
    }
	Start-Service $DirectoryName -ErrorVariable err
    if ($err -ne $null)
    {
        throw "Exception starting SmsEmailSender service: $err"
    }
    else{
        echo "Service $DirectoryName started successfully"
    }
}

function InstallEndpoints
{
	if (!([System.IO.Directory]::Exists($installFolder)))
	{
        [System.IO.Directory]::CreateDirectory($installFolder)
        echo "Creating $installFolder"
	}

    InstallService "SmsCoordinator" "Service for coordinating sms"
    InstallService "SmsScheduler" "Service for scheduling sms messages"
    InstallService "SmsActioner" "Service for delivering sms"
}

function InstallWeb
{
    $msDeploy = "C:\Program Files (x86)\IIS\Microsoft Web Deploy V2\msdeploy.exe"
    $webDeployPackage = Join-Path $build_output -childpath '\SmsWeb.zip'
    
    $environmentParametersFile
    if ($go_environment -ne $null)
    {
        Write-Host "Go environment set to $go_environment, copying appropriate web.config"
        $environmentConfig = $build_output + '\Configuration\' + $go_environment + '.SmsWeb.SetParameters.xml'
        $environmentParametersFile = $build_output + 'SmsWeb.SetParameters.xml'
        Copy-Item $environmentConfig $environmentParametersFile
    }
    else
    {
        Write-Host "No Go environment set in $go_environment, leaving default web.config"
    }
    
    echo $webDeployPackage
    $arg = " -verb:sync -source:package='$webDeployPackage' -dest:auto -verbose -setParamFile=""$environmentParametersFile"""
    
    echo $arg
    RunCommand $msdeploy $arg
}

function RunCommand([string]$fileName, [string]$arg)
{
    $ps = new-object System.Diagnostics.Process
    $ps.StartInfo.Filename = $fileName
    $ps.StartInfo.Arguments = $arg
    $ps.StartInfo.RedirectStandardOutput = $True
    $ps.StartInfo.RedirectStandardError = $True
    $ps.StartInfo.UseShellExecute = $false
    $null = $ps.Start()
    [string] $Out = $ps.StandardOutput.ReadToEnd();
    [string] $Err = $ps.StandardError.ReadToEnd();
    $exitCode = $ps.ExitCode
    $exitCode
    $Out
    $Err
    if (!($exitCode -eq 0))
	{
		throw ("Errors Running Command" + $fileName + $arg + "`r`n" + $Err)
	}
    return
}

function SetupInfrastructure
{
    $nserviceBusCore = Join-Path $build_output -childpath "SmsCoordinator\NServiceBus.Core.dll"
	Import-Module $nserviceBusCore
	Install-Dtc
	Install-Msmq
	Install-RavenDB
	Install-PerformanceCounters
}

function Build
{
	$clean = $msbuild + " " + $path + "\SmsScheduler.sln /p:Configuration=Release /t:Clean"
	$build = $msbuild + " " + $path + "\SmsScheduler.sln /p:Configuration=Release /p:VisualStudioVersion=12.0 /t:Build"
    $webPackage = $msbuild + " " + $path + "\SmsWeb\SmsWeb.csproj /p:Configuration=Release /verbosity:detailed /t:Package"

	Invoke-Expression $clean
	Invoke-Expression $build
    Invoke-Expression $webPackage
    $installFiles = Join-Path $path -childpath '\Install*.*'
    $configFiles = Join-Path $path -childpath '\Configuration\*.*'
    $configDestination = Join-Path $build_output -childpath '\Configuration'
    
    if ([System.IO.Directory]::Exists($configDestination))
	{
	 [System.IO.Directory]::Delete($configDestination, 1)
	}
	[System.IO.Directory]::CreateDirectory($configDestination)
    
    Copy-Item $installFiles $build_output
    Copy-Item $configFiles $configDestination
}

function UnitTests
{
	$nunit = $path + "\packages\NUnit.Runners.2.6.2\tools\nunit-console.exe"

	$EmailSender = Join-Path $build_output -childpath "tests\EmailSenderTests\EmailSenderTests.dll"
	$SmsCoordinatorTests = Join-Path $build_output -childpath "tests\SmsCoordinatorTests\SmsCoordinatorTests.dll"
	$SmsTrackingTests = Join-Path $build_output -childpath "tests\SmsTrackingTests\SmsTrackingTests.dll"
	$SmsWebTests = Join-Path $build_output -childpath "tests\SmsWebTests\SmsWebTests.dll"

	$xmlResultsFile = Join-Path $path -childpath "TestResult.xml"
	& $nunit /xml:$xmlResultsFile $EmailSender $SmsCoordinatorTests $SmsTrackingTests $SmsWebTests 

	[xml]$testOutput = Get-Content $xmlResultsFile
	$failureCount = $testOutput.'test-results'.failures
	if ($failureCount > 0)
	{
		throw "Tests Failure"
	}
}

function StopEndpoints
{
	if(Get-Service "SmsCoordinator" -ErrorAction SilentlyContinue)
	{
		"Stopping service SmsCoordinator"
		Stop-Service SmsCoordinator
	}

	if(Get-Service "SmsScheduler" -ErrorAction SilentlyContinue)
	{
		"Stopping service SmsScheduler"
		Stop-Service SmsScheduler
	}

	if(Get-Service "SmsActioner" -ErrorAction SilentlyContinue)
	{
		"Stopping service SmsActioner"
		Stop-Service SmsActioner
	}
}

function StopEndpoints
{
	if(Get-Service "SmsCoordinator" -ErrorAction SilentlyContinue)
	{
		"Stopping service SmsCoordinator"
		Stop-Service SmsCoordinator
	}

	if(Get-Service "SmsScheduler" -ErrorAction SilentlyContinue)
	{
		"Stopping service SmsScheduler"
		Stop-Service SmsScheduler
	}

	if(Get-Service "SmsActioner" -ErrorAction SilentlyContinue)
	{
		"Stopping service SmsActioner"
		Stop-Service SmsActioner
	}
}