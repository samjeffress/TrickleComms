using System;
using System.Collections.Generic;
using NServiceBus;

namespace SmsMessages
{
    public class TrickleSmsOverTimePeriod : ICommand
    {
        public List<SmsData> Messages { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan Duration { get; set; }
    }

    public class TrickleSmsSpacedByTimePeriod : ICommand
    {
        public List<SmsData> Messages { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan TimeSpacing { get; set; }
    }
}