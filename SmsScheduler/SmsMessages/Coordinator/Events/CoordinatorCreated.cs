using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator.Events
{
    public class CoordinatorCreated
    {
        public CoordinatorCreated()
        {
            ConfirmationEmailAddresses = new List<string>();
        }

        public Guid CoordinatorId { get; set; }

        public List<MessageSchedule> ScheduledMessages { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public SmsMetaData MetaData { get; set; }

        public List<string> ConfirmationEmailAddresses { get; set; }

        public string UserOlsenTimeZone { get; set; }

        public string MessageBody { get; set; }

        public int MessageCount { get; set; }
    }

    public class MessageSchedule
    {
        public string Number { get; set; }

        public DateTime ScheduledTimeUtc { get; set; }

        public Guid ScheduleMessageId { get; set; }
    }
}