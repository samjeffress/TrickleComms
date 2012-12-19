using System;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Events
{
    public class ScheduledSmsFailed
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduledSmsId { get; set; }

        public SmsFailed SmsFailedData { get; set; }

        public string Number { get; set; }
    }
}