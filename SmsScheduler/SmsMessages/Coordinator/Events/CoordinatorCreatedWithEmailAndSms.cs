using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator.Events
{
    public class CoordinatorCreatedWithEmailAndSms
    {
        public CoordinatorCreatedWithEmailAndSms()
        {
            ConfirmationEmailAddresses = new List<string>();
            EmailData = new EmailData();
        }

        public Guid CoordinatorId { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public SmsMetaData MetaData { get; set; }

        public EmailData EmailData { get; set; }

        public string SmsMessage { get; set; }

        public int SmsCount { get; set; }

        public int EmailCount { get; set; }

        public List<string> ConfirmationEmailAddresses { get; set; }

        public string UserOlsenTimeZone { get; set; }

        public string UserName { get; set; }
    }
}