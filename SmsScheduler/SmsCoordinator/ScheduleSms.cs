using System;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages;
using SmsMessages.Commands;
using SmsMessages.Events;

namespace SmsCoordinator
{
    public class ScheduleSms : 
        Saga<ScheduledSmsData>,
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
            Data.OriginalMessage = scheduleSmsForSendingLater;
            RequestUtcTimeout<ScheduleSmsTimeout>(scheduleSmsForSendingLater.SendMessageAt);
        }

        public void Timeout(ScheduleSmsTimeout state)
        {
            var sendOneMessageNow = new SendOneMessageNow
            {
                CorrelationId = Data.Id,
                SmsData = Data.OriginalMessage.SmsData,
                SmsMetaData = Data.OriginalMessage.SmsMetaData
            };
            Bus.Send(sendOneMessageNow);
        }

        public void Handle(MessageSent scheduleSmsForSendingLater)
        {
            MarkAsComplete();
        }
    }

    public class ScheduledSmsData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public ScheduleSmsForSendingLater OriginalMessage { get; set; }
    }

    public class ScheduleSmsTimeout
    {
    }
}