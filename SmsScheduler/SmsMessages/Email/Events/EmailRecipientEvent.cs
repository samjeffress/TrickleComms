using System;

namespace SmsMessages.Email.Events
{
	public class EmailRecipientEvent
	{
		public string EmailId { get; set; }
		public EmailStatus Status { get; set; }
		public DateTime ActionTime { get; set; }
	}
}

