using System;
using NServiceBus;
using SmsMessages.Tracking.Scheduling.Commands;

namespace SmsScheduler
{
    public class ScheduleStatusHandlers :
        IHandleMessages<ScheduleStatusChanged>,
        IHandleMessages<ScheduleSucceeded>
    {
        public IRavenDocStore RavenDocStore { get; set; }

        public void Handle(ScheduleStatusChanged message)
        {
            throw new NotImplementedException();
        }

        public void Handle(ScheduleSucceeded message)
        {
            throw new NotImplementedException();
        }

        public void Handle(ScheduleFailed message)
        {
            throw new NotImplementedException();
        }
    }
}