using System;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages;

namespace SmsCoordinator
{
    public class SmsScheduler : 
        Saga<SmsSchedulerData>,
        IAmStartedByMessages<ScheduleSmsForSendingLater>,
        IHandleTimeouts<ScheduleSmsTimeout>,
        IHandleMessages<MessageSent>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<MessageSent>(data => data.Id, message => message.CorrelationId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(ScheduleSmsForSendingLater scheduleSmsForSendingLater)
        {
            RequestUtcTimeout<ScheduleSmsTimeout>(scheduleSmsForSendingLater.SendMessageAt);
        }

        public void Timeout(ScheduleSmsTimeout state)
        {
            Bus.Send(new SendOneMessageNow());
        }

        public void Handle(MessageSent scheduleSmsForSendingLater)
        {
            MarkAsComplete();
        }
    }

    public class SmsSchedulerData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    public class ScheduleSmsTimeout
    {
    }
}