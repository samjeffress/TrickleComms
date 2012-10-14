using System;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling
{
    public class ScheduleCreated : IMessage
    {
        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleId { get; set; }

        public Guid CallerId { get; set; }
    }

    public class SchedulePaused : IMessage
    {
        public Guid ScheduleId { get; set; }
    }

    public class ScheduleResumed : IMessage
    {
        public Guid ScheduleId { get; set; }
    }

    public class ScheduleCancelled : IMessage
    {
        public Guid ScheduleId { get; set; }
    }

    public class ScheduleComplete : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}