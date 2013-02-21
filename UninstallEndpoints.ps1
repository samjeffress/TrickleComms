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