using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator.Commands
{
    public class SendAllMessagesAtOnce
    {
        public Guid CoordinatorId { get; set; }

        public List<SmsData> Messages { get; set; }

        public DateTime SendTimeUtc { get; set; }

        public SmsMetaData MetaData { get; set; }

        public string ConfirmationEmail { get; set; }

        public string UserOlsenTimeZone { get; set; }
    }
}