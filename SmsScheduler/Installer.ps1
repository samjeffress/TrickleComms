
function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$installFolder = "c:\SmsServices"
$path = Get-ScriptDirectory Installer.ps1
$build_output = (Get-Item $path).parent.FullName + '\build_output\'

function InstallEndpoints
{
	if ([System.IO.Directory]::Exists($installFolder))
	{
	 [System.IO.Directory]::Delete($installFolder, 1)
	}
	[System.IO.Directory]::CreateDirectory($installFolder)

	# move all the service files that were built to the output folder
	[System.IO.Directory]::Move($build_output + 'EmailSender', $installFolder + '\EmailSender')
	[System.IO.Directory]::Move($build_output + 'SmsCoordinator', $installFolder + '\SmsCoordinator')
	[System.IO.Directory]::Move($build_output + 'SmsTracking', $installFolder + '\SmsTracking')

	$nsbHost = Join-Path $installFolder -childpath  '\EmailSender\NServiceBus.Host.exe'
	& $nsbHost ("/install", "/serviceName:SmsEmailSender", "/displayName:Sms Email Sender", "/description:Service for sending emails from Sms Coordinator", "NServiceBus.Production")
	Start-Service SmsEmailSender

	$nsbHost = Join-Path $installFolder -childpath '\SmsCoordinator\NServiceBus.Host.exe'
	& $nsbHost ("/install", "/serviceName:SmsCoordinator", "/displayName:Sms Coordinator", "/description:Service for coordinating and sending Sms", "NServiceBus.Production")
	Start-Service SmsCoordinator

	#.\build_output\SmsTracking\NServiceBus.Host.exe /install /serviceName:"SmsTracking" /displayName:"Sms Tracking" /description:"Service for tracking status of coordinated and Sms"
	$nsbHost = Join-Path $installFolder -childpath '\SmsTracking\NServiceBus.Host.exe'
	& $nsbHost ("/install", "/serviceName:SmsTracking", "/displayName:Sms Tracking",  "/description:Service for tracking status of coordinated and Sms", "NServiceBus.Production")
	Start-Service SmsTracking
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
	$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

	$clean = $msbuild + " " + $path + "\SmsScheduler.sln /p:Configuration=Release /t:Clean"
	$build = $msbuild + " " + $path + "\SmsScheduler.sln /p:Configuration=Release /t:Build"

	Invoke-Expression $clean
	Invoke-Expression $build
    $installFiles = Join-Path $path -childpath '\Install*.*'
    Copy-Item $installFiles $build_output
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

function UninstallEndpoints
{
	if(Get-Service "SmsEmailSender" -ErrorAction SilentlyContinue)
	{
		Stop-Service SmsEmailSender
		"Service Exists (SmsEmailSender) - Uninstalling"
		$service = Get-WmiObject -Class Win32_Service -Filter "Name='SmsEmailSender'"
		$service.delete()
	}

	if(Get-Service "SmsCoordinator" -ErrorAction SilentlyContinue)
	{
		Stop-Service SmsCoordinator
		"Service Exists (SmsCoordinator) - Uninstalling"
		$service = Get-WmiObject -Class Win32_Service -Filter "Name='SmsCoordinator'"
		$service.delete()
	}

	if(Get-Service "SmsTracking" -ErrorAction SilentlyContinue)
	{
		Stop-Service SmsTracking
		"Service Exists (SmsTracking) - Uninstalling"
		$service = Get-WmiObject -Class Win32_Service -Filter "Name='SmsTracking'"
		$service.delete()
	}
}