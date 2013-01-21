using System;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;

namespace SmsCoordinator
{
    public class ScheduleSms : 
        Saga<ScheduledSmsData>,
        IAmStartedByMessages<ScheduleSmsForSendingLater>,
        IHandleTimeouts<ScheduleSmsTimeout>,
        IHandleMessages<PauseScheduledMessageIndefinitely>,
        IHandleMessages<ResumeScheduledMessageWithOffset>,
        IHandleMessages<MessageSent>,
        IHandleMessages<MessageFailedSending>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<MessageSent>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<MessageFailedSending>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<PauseScheduledMessageIndefinitely>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            ConfigureMapping<ResumeScheduledMessageWithOffset>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(ScheduleSmsForSendingLater scheduleSmsForSendingLater)
        {
            Data.OriginalMessage = scheduleSmsForSendingLater;
            Data.ScheduleMessageId = scheduleSmsForSendingLater.ScheduleMessageId == Guid.NewGuid() ? Data.Id : scheduleSmsForSendingLater.ScheduleMessageId;
            Data.RequestingCoordinatorId = scheduleSmsForSendingLater.CorrelationId;
            var timeout = new DateTime(scheduleSmsForSendingLater.SendMessageAtUtc.Ticks, DateTimeKind.Utc);
            RequestUtcTimeout<ScheduleSmsTimeout>(timeout);
            Bus.Publish(new SmsScheduled
            {
                ScheduleMessageId = Data.ScheduleMessageId, 
                CoordinatorId = scheduleSmsForSendingLater.CorrelationId,
                SmsData = scheduleSmsForSendingLater.SmsData,
                SmsMetaData = scheduleSmsForSendingLater.SmsMetaData,
                ScheduleSendingTimeUtc = scheduleSmsForSendingLater.SendMessageAtUtc
            });
            //Bus.Send(new ScheduleCreated
            //{
            //    CallerId = Data.Id,
            //    ScheduleId = Data.ScheduleMessageId,
            //    SmsData = scheduleSmsForSendingLater.SmsData,
            //    SmsMetaData = scheduleSmsForSendingLater.SmsMetaData
            //});
        }

        public void Timeout(ScheduleSmsTimeout state)
        {
            if (!Data.SchedulingPaused)
            {
                var sendOneMessageNow = new SendOneMessageNow
                {
                    CorrelationId = Data.Id,
                    SmsData = Data.OriginalMessage.SmsData,
                    SmsMetaData = Data.OriginalMessage.SmsMetaData,
                    ConfirmationEmailAddress = Data.OriginalMessage.ConfirmationEmail
                };
                Bus.Send(sendOneMessageNow);
            }
        }

        public void Handle(MessageSent message)
        {
            Bus.Publish(new ScheduledSmsSent { CoordinatorId = Data.RequestingCoordinatorId, ScheduledSmsId = Data.ScheduleMessageId, ConfirmationData = message.ConfirmationData, Number = message.SmsData.Mobile});
            //Bus.Send(new ScheduleComplete {ScheduleId = Data.ScheduleMessageId});
            MarkAsComplete();
        }

        public void Handle(PauseScheduledMessageIndefinitely pauseScheduling)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > pauseScheduling.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = true;
            //var schedulePaused = new SchedulePaused {ScheduleId = pauseScheduling.ScheduleMessageId};
            //Bus.Send(schedulePaused);
            Bus.Publish(new MessageSchedulePaused { CoordinatorId = Data.RequestingCoordinatorId, ScheduleId = pauseScheduling.ScheduleMessageId });
            Data.LastUpdateCommandRequestUtc = pauseScheduling.MessageRequestTimeUtc;
        }

        public void Handle(ResumeScheduledMessageWithOffset scheduleSmsForSendingLater)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > scheduleSmsForSendingLater.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = false;
            var rescheduledTime = Data.OriginalMessage.SendMessageAtUtc.Add(scheduleSmsForSendingLater.Offset);
            RequestUtcTimeout<ScheduleSmsTimeout>(rescheduledTime);
            //Bus.Send(new ScheduleResumed {ScheduleId = Data.ScheduleMessageId, RescheduledTime = rescheduledTime});
            Bus.Publish(new MessageRescheduled { CoordinatorId = Data.RequestingCoordinatorId, ScheduleMessageId = Data.ScheduleMessageId, RescheduledTimeUtc = rescheduledTime });
            Data.LastUpdateCommandRequestUtc = scheduleSmsForSendingLater.MessageRequestTimeUtc;
        }

        public void Handle(MessageFailedSending failedMessage)
        {
            Bus.Publish(new ScheduledSmsFailed
                            {
                                CoordinatorId = Data.RequestingCoordinatorId, 
                                ScheduledSmsId = Data.ScheduleMessageId, 
                                Number = failedMessage.SmsData.Mobile,
                                SmsFailedData = failedMessage.SmsFailed
                            });
            //Bus.Send(new ScheduleFailed { ScheduleId = Data.ScheduleMessageId });
            MarkAsComplete();
        }
    }

    public class ScheduledSmsData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public ScheduleSmsForSendingLater OriginalMessage { get; set; }

        public bool SchedulingPaused { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public DateTime? LastUpdateCommandRequestUtc { get; set; }

        public Guid RequestingCoordinatorId { get; set; }
    }

    public class ScheduleSmsTimeout
    {
    }
}