using System;
using System.Collections.Generic;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator
{
    public class TrickleSmsOverCalculatedIntervalsBetweenSetDates : ICommand
    {
        public Guid CoordinatorId { get; set; }

        public List<SmsData> Messages { get; set; }

        public DateTime StartTimeUTC { get; set; }

        public TimeSpan Duration { get; set; }

        public SmsMetaData MetaData { get; set; }
    }
}