using System;
using SmsMessages.CommonData;

namespace SmsMessages.Tracking.Scheduling.Commands
{
    public class ScheduleSucceeded
    {
        public Guid ScheduleId { get; set; }

        public SmsConfirmationData ConfirmationData { get; set; }
    }
}
