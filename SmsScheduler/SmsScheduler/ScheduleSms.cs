using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;
using SmsMessages.MessageSending.Responses;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;
using SmsMessages.Tracking.Scheduling.Commands;

namespace SmsScheduler
{
    public class ScheduleSms : 
        Saga<ScheduledSmsData>,
        IAmStartedByMessages<ScheduleSmsForSendingLater>,
        IHandleTimeouts<ScheduleSmsTimeout>,
        IHandleMessages<PauseScheduledMessageIndefinitely>,
        IHandleMessages<ResumeScheduledMessageWithOffset>,
        IHandleMessages<RescheduleScheduledMessageWithNewTime>,
        IHandleMessages<MessageSuccessfullyDelivered>,
        IHandleMessages<MessageFailedSending>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<MessageSuccessfullyDelivered>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<MessageFailedSending>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<PauseScheduledMessageIndefinitely>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            ConfigureMapping<ResumeScheduledMessageWithOffset>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            ConfigureMapping<RescheduleScheduledMessageWithNewTime>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(ScheduleSmsForSendingLater scheduleSmsForSendingLater)
        {
            Data.OriginalMessageData = new OriginalMessageData(scheduleSmsForSendingLater);
            Data.OriginalMessage = scheduleSmsForSendingLater;
            Data.ScheduleMessageId = scheduleSmsForSendingLater.ScheduleMessageId == Guid.NewGuid() ? Data.Id : scheduleSmsForSendingLater.ScheduleMessageId;
            Data.RequestingCoordinatorId = scheduleSmsForSendingLater.CorrelationId;
            Data.TimeoutCounter = 0;
            var timeout = new DateTime(scheduleSmsForSendingLater.SendMessageAtUtc.Ticks, DateTimeKind.Utc);
            RequestUtcTimeout(timeout, new ScheduleSmsTimeout { TimeoutCounter = 0});
            Bus.SendLocal(new ScheduleCreated
                {
                    ScheduleId = Data.ScheduleMessageId,
                    ScheduleTimeUtc = scheduleSmsForSendingLater.SendMessageAtUtc,
                    SmsData = scheduleSmsForSendingLater.SmsData,
                    SmsMetaData = scheduleSmsForSendingLater.SmsMetaData
                });
            Bus.Publish(new SmsScheduled
            {
                ScheduleMessageId = Data.ScheduleMessageId, 
                CoordinatorId = scheduleSmsForSendingLater.CorrelationId,
                SmsData = scheduleSmsForSendingLater.SmsData,
                SmsMetaData = scheduleSmsForSendingLater.SmsMetaData,
                ScheduleSendingTimeUtc = scheduleSmsForSendingLater.SendMessageAtUtc
            });
        }

        public void Timeout(ScheduleSmsTimeout state)
        {
            if (!Data.SchedulingPaused && state.TimeoutCounter == Data.TimeoutCounter)
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

        public void Handle(MessageSuccessfullyDelivered message)
        {
            var originalMessage = Data.OriginalMessageData;
            Bus.Publish(new ScheduledSmsSent
                {
                    CoordinatorId = originalMessage.RequestingCoordinatorId, 
                    ScheduledSmsId = Data.ScheduleMessageId, 
                    ConfirmationData = message.ConfirmationData, 
                    Number = message.SmsData.Mobile,
                    Username = originalMessage.Username
                });
            Bus.SendLocal(new ScheduleSucceeded
                {
                    ScheduleId = Data.ScheduleMessageId,
                    ConfirmationData = message.ConfirmationData
                });

            MarkAsComplete();
        }

        public void Handle(PauseScheduledMessageIndefinitely pauseScheduling)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > pauseScheduling.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = true;
            Bus.SendLocal(new ScheduleStatusChanged
                {
                    ScheduleId = pauseScheduling.ScheduleMessageId,
                    RequestTimeUtc = pauseScheduling.MessageRequestTimeUtc,
                    Status = MessageStatus.Paused
                });
            var originalMessage = Data.OriginalMessageData;
            Bus.Publish(new MessageSchedulePaused
                {
                    CoordinatorId = originalMessage.RequestingCoordinatorId, 
                    ScheduleId = pauseScheduling.ScheduleMessageId,
                    Number = originalMessage.Number
                });
            Data.LastUpdateCommandRequestUtc = pauseScheduling.MessageRequestTimeUtc;
        }

        public void Handle(ResumeScheduledMessageWithOffset scheduleSmsForSendingLater)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > scheduleSmsForSendingLater.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = false;
            var rescheduledTime = Data.OriginalMessage.SendMessageAtUtc.Add(scheduleSmsForSendingLater.Offset);
            Data.TimeoutCounter++;
            RequestUtcTimeout(rescheduledTime, new ScheduleSmsTimeout { TimeoutCounter = Data.TimeoutCounter });
            Bus.Publish(new MessageRescheduled
                {
                    CoordinatorId = Data.OriginalMessageData.RequestingCoordinatorId, 
                    ScheduleMessageId = Data.ScheduleMessageId, 
                    RescheduledTimeUtc = rescheduledTime, 
                    Number = Data.OriginalMessageData.Number
                });
            Bus.SendLocal(new ScheduleStatusChanged
                {
                    RequestTimeUtc = scheduleSmsForSendingLater.MessageRequestTimeUtc,
                    ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId,
                    ScheduleTimeUtc = rescheduledTime,
                    Status = MessageStatus.Scheduled
                });
            Data.LastUpdateCommandRequestUtc = scheduleSmsForSendingLater.MessageRequestTimeUtc;
        }

        public void Handle(RescheduleScheduledMessageWithNewTime message)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > message.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = false;
            Data.TimeoutCounter++;
            RequestUtcTimeout(message.NewScheduleTimeUtc, new ScheduleSmsTimeout { TimeoutCounter = Data.TimeoutCounter });
            Bus.SendLocal(new ScheduleStatusChanged
                {
                    RequestTimeUtc = message.MessageRequestTimeUtc,
                    ScheduleId = message.ScheduleMessageId,
                    Status = MessageStatus.Scheduled,
                    ScheduleTimeUtc = message.NewScheduleTimeUtc
                });
            Bus.Publish(new MessageRescheduled
                {
                    CoordinatorId = Data.OriginalMessageData.RequestingCoordinatorId, 
                    ScheduleMessageId = Data.ScheduleMessageId, 
                    RescheduledTimeUtc = message.NewScheduleTimeUtc, 
                    Number = Data.OriginalMessageData.Number
                });
            Data.LastUpdateCommandRequestUtc = message.MessageRequestTimeUtc;
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

            Bus.SendLocal(new ScheduleFailed
                {
                    ScheduleId = Data.ScheduleMessageId,
                    Code = failedMessage.SmsFailed.Code,
                    Message = failedMessage.SmsFailed.Message,
                    MoreInfo = failedMessage.SmsFailed.MoreInfo,
                    Status = failedMessage.SmsFailed.Status
                });
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

        public int TimeoutCounter { get; set; }

        public OriginalMessageData OriginalMessageData { get; set; }
    }


    public class OriginalMessageData
    {
        public OriginalMessageData() { }

        public OriginalMessageData(ScheduleSmsForSendingLater requestMessage)
        {
            RequestingCoordinatorId = requestMessage.CorrelationId;
            Username = requestMessage.Username;
            Number = requestMessage.SmsData.Mobile;
            Message = requestMessage.SmsData.Message;
            Topic = requestMessage.SmsMetaData.Topic;
            Tags = requestMessage.SmsMetaData.Tags;
            ConfirmationEmail = requestMessage.ConfirmationEmail;
            OriginalRequestSendTime = requestMessage.SendMessageAtUtc;
        }

        public Guid RequestingCoordinatorId { get; set; }
        public string Username { get; set; }
        public string Number { get; set; }
        public string Message { get; set; }
        public string Topic { get; set; }
        public IList<string> Tags { get; set; }
        public string ConfirmationEmail { get; set; }
        public DateTime OriginalRequestSendTime { get; set; }
    }

    public class ScheduleSmsTimeout
    {
        public int TimeoutCounter { get; set; }
    }
}
