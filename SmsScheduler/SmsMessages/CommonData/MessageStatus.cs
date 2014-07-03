namespace SmsMessages.CommonData
{
    public enum MessageStatus
    {
        WaitingForScheduling,
        Scheduled,
        Sent,
        Paused,
        Cancelled,
        Failed,
        /// <summary>
        /// Delivered to email server successfully - does not apply to SMS
        /// </summary>
        Delivered
    }
}