using System;
using System.Collections.Generic;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator
{
    public class TrickleSmsOverTimePeriod : ICommand
    {
        public List<SmsData> Messages { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public SmsMetaData MetaData { get; set; }
    }
}