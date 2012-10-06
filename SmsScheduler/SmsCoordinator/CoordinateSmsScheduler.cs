using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages;

namespace SmsCoordinator
{
    public class CoordinateSmsScheduler : 
        Saga<CoordinateSmsSchedulingData>,
        IAmStartedByMessages<TrickleSmsOverTimePeriod>, 
        IAmStartedByMessages<TrickleSmsSpacedByTimePeriod>,
        IHandleMessages<ScheduledSmsSent>
    {
        public ICalculateSmsTiming TimingManager { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<ScheduledSmsSent>(data => data.Id, message => message.CoordinatorId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(TrickleSmsOverTimePeriod message)
        {
            var messageTiming = TimingManager.CalculateTiming(message.StartTime, message.Duration, message.Messages.Count);
            for(int i = 0; i < message.Messages.Count; i++)
            {
                var smsForSendingLater = new ScheduleSmsForSendingLater {SendMessageAt = messageTiming[i]};
                Bus.Send(smsForSendingLater);
                Data.MessagesScheduled++;
            }
        }

        public void Handle(TrickleSmsSpacedByTimePeriod trickleMultipleMessages)
        {
            for(int i = 0; i < trickleMultipleMessages.Messages.Count; i++)
            {
                var extraTime = TimeSpan.FromTicks(trickleMultipleMessages.TimeSpacing.Ticks*i);
                var smsForSendingLater = new ScheduleSmsForSendingLater
                {
                    SendMessageAt = trickleMultipleMessages.StartTime.Add(extraTime)
                };
                Bus.Send(smsForSendingLater);
                Data.MessagesScheduled++;
            }
        }

        public void Handle(ScheduledSmsSent smsSent)
        {
            Data.MessagesConfirmedSent++;
            if (Data.MessagesScheduled == Data.MessagesConfirmedSent)
                MarkAsComplete();
        }
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

        public int MessagesScheduled { get; set; }

        public int MessagesConfirmedSent { get; set; }
    }
}