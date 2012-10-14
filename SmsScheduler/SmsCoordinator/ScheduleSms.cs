using System;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.MessageSending;
using SmsMessages.Scheduling;

namespace SmsCoordinator
{
    public class ScheduleSms : 
        Saga<ScheduledSmsData>,
        IAmStartedByMessages<ScheduleSmsForSendingLater>,
        IHandleTimeouts<ScheduleSmsTimeout>,
        IHandleMessages<PauseScheduledMessageIndefinitely>,
        IHandleMessages<ResumeScheduledMessageWithOffset>,
        IHandleMessages<MessageSent>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<MessageSent>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<PauseScheduledMessageIndefinitely>(data => data.OriginalMessage.ScheduleMessageId, message => message.ScheduleMessageId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(ScheduleSmsForSendingLater scheduleSmsForSendingLater)
        {
            Data.OriginalMessage = scheduleSmsForSendingLater;
            RequestUtcTimeout<ScheduleSmsTimeout>(scheduleSmsForSendingLater.SendMessageAt);
            ReplyToOriginator(new SmsScheduled { ScheduleMessageId = scheduleSmsForSendingLater.ScheduleMessageId, CoordinatorId = scheduleSmsForSendingLater.CorrelationId });
        }

        public void Timeout(ScheduleSmsTimeout state)
        {
            if (!Data.SchedulingPaused)
            {
                var sendOneMessageNow = new SendOneMessageNow
                {
                    CorrelationId = Data.Id,
                    SmsData = Data.OriginalMessage.SmsData,
                    SmsMetaData = Data.OriginalMessage.SmsMetaData
                };
                Bus.Send(sendOneMessageNow);
            }
        }

        public void Handle(MessageSent scheduleSmsForSendingLater)
        {
            ReplyToOriginator(new ScheduledSmsSent { CoordinatorId = Guid.Parse(Data.OriginalMessageId), ScheduledSmsId = Data.OriginalMessage.ScheduleMessageId });
            MarkAsComplete();
        }

        public void Handle(PauseScheduledMessageIndefinitely pauseScheduling)
        {
            Data.SchedulingPaused = true;
        }

        public void Handle(ResumeScheduledMessageWithOffset scheduleSmsForSendingLater)
        {
            Data.SchedulingPaused = false;
            RequestUtcTimeout<ScheduleSmsTimeout>(Data.OriginalMessage.SendMessageAt.Add(scheduleSmsForSendingLater.Offset));
        }
    }

    public class ScheduledSmsData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public ScheduleSmsForSendingLater OriginalMessage { get; set; }

        public bool SchedulingPaused { get; set; }
    }

    public class ScheduleSmsTimeout
    {
    }
}