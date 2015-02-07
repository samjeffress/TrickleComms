using System;

namespace SmsMessages.Email.Events
{
	[Obsolete("Using webhooks, so don't need to have internal messages as we aren't doing timeouts")]
    public class EmailSent
    {
        public string EmailAddress { get; set; }

        public string BodyHtml { get; set; }

        public string BodyText { get; set; }

        public string Subject { get; set; }

        public Guid Id { get; set; }

        public DateTime SendTimeUtc { get; set; }
    }
}
