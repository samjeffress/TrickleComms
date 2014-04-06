using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator.Commands
{
    public class TrickleSmsAndEmailBetweenSetTimes
    {
        public TrickleSmsAndEmailBetweenSetTimes()
        {
            ConfirmationEmails = new List<string>();
        }

        public Guid CoordinatorId { get; set; }

        public string SmsAndEmailDataId { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public TimeSpan Duration { get; set; }

        public SmsMetaData MetaData { get; set; }

        public List<string> ConfirmationEmails { get; set; }

        public string UserOlsenTimeZone { get; set; }

        public string Username { get; set; }
    }
}