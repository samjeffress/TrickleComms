using System;
using System.Collections.Generic;
using NServiceBus.Saga;
using SmsMessages;

namespace SmsCoordinator
{
    public class CoordinateSmsScheduler : 
        Saga<CoordinateSmsSchedulingData>,
        IAmStartedByMessages<TrickleSmsOverTimePeriod>
    {
        public void Handle(TrickleSmsOverTimePeriod message)
        {
            var messageTiming = TimingManager.CalculateTiming(message.StartTime, message.Duration, message.Messages.Count);
            for(int i = 0; i < message.Messages.Count; i++)
            {
                var smsForSendingLater = new ScheduleSmsForSendingLater {SendMessageAt = messageTiming[i]};
                Bus.Send(smsForSendingLater);
            }
        }

        public ICalculateSmsTiming TimingManager { get; set; }
    }

    public interface ICalculateSmsTiming
    {
        List<DateTime> CalculateTiming(DateTime startTime, TimeSpan duration, int i);
    }

    public class CoordinateSmsSchedulingData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}