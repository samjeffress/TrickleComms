using System;
using System.Collections.Generic;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator
{
    public class TrickleSmsWithDefinedTimeBetweenEachMessage : ICommand
    {
        public Guid CoordinatorId { get; set; }

        public List<SmsData> Messages { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan TimeSpacing { get; set; }

        public SmsMetaData MetaData { get; set; }
    }
}