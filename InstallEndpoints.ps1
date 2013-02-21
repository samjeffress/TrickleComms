$installFolder = "c:\SmsServices"

if ([System.IO.Directory]::Exists($installFolder))
{
 [System.IO.Directory]::Delete($installFolder, 1)
}
[System.IO.Directory]::CreateDirectory($installFolder)

# move all the service files that were built to the output folder
[System.IO.Directory]::Move('.\build_output\EmailSender', $installFolder + '\EmailSender')
[System.IO.Directory]::Move('.\build_output\SmsCoordinator', $installFolder + '\SmsCoordinator')
[System.IO.Directory]::Move('.\build_output\SmsTracking', $installFolder + '\SmsTracking')

$nsbHost = $installFolder + '\SmsCoordinator\NServiceBus.Host.exe'
& $nsbHost ("/install", "/serviceName:SmsEmailSender", "/displayName:Sms Email Sender", "/description:Service for sending emails from Sms Coordinator", "NServiceBus.Production")
Start-Service SmsEmailSender

$nsbHost = $installFolder + '\SmsCoordinator\NServiceBus.Host.exe'
& $nsbHost ("/install", "/serviceName:SmsCoordinator", "/displayName:Sms Coordinator", "/description:Service for coordinating and sending Sms", "NServiceBus.Production")
Start-Service SmsCoordinator

#.\build_output\SmsTracking\NServiceBus.Host.exe /install /serviceName:"SmsTracking" /displayName:"Sms Tracking" /description:"Service for tracking status of coordinated and Sms"
$nsbHost = $installFolder + '\SmsTracking\NServiceBus.Host.exe'
& $nsbHost ("/install", "/serviceName:SmsTracking", "/displayName:Sms Tracking",  "/description:Service for tracking status of coordinated and Sms", "NServiceBus.Production")
Start-Service SmsTracking