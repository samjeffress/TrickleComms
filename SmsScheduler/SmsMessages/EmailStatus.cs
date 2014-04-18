namespace SmsMessages
{
    public enum EmailStatus
    {
        /// <summary>
        /// Mailgun accepted the request to send/forward the email and the message has been placed in queue.
        /// </summary>
        Accepted, 

        /// <summary>
        /// Mailgun rejected the request to send/forward the email.
        /// </summary>
        Rejected,

        /// <summary>
        /// Mailgun sent the email and it was accepted by the recipient email server.
        /// </summary>
        Delivered,

        /// <summary>
        /// Mailgun could not deliver the email to the recipient email server.
        /// </summary>
        Failed,

        /// <summary>
        /// The email recipient opened the email and enabled image viewing. Open tracking must be enabled in the Mailgun control panel, and the CNAME record must be pointing to mailgun.org.
        /// </summary>
        Opened,

        /// <summary>
        /// The email recipient clicked on a link in the email. Click tracking must be enabled in the Mailgun control panel, and the CNAME record must be pointing to mailgun.org.
        /// </summary>
        Clicked,
        
        /// <summary>
        /// The email recipient clicked on the unsubscribe link. Unsubscribe tracking must be enabled in the Mailgun control panel.
        /// </summary>
        Unsubscribed,

        /// <summary>
        /// The email recipient clicked on the spam complaint button within their email client. Feedback loops enable the notification to be received by Mailgun.
        /// </summary>
        Complained,
    }
}