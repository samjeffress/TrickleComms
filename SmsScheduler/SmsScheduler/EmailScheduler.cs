using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Responses;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;
using SmsMessages.Tracking.Scheduling.Commands;

namespace SmsScheduler
{
    public class EmailScheduler :
        Saga<ScheduledEmailData>,
        IAmStartedByMessages<ScheduleEmailForSendingLater>,
        IHandleTimeouts<ScheduleEmailTimeout>,
        IHandleMessages<PauseScheduledMessageIndefinitely>,
        IHandleMessages<ResumeScheduledMessageWithOffset>,
        IHandleMessages<RescheduleScheduledMessageWithNewTime>,
        IHandleMessages<EmailStatusUpdate>
    {
        public void Handle(ScheduleEmailForSendingLater message)
        {
            Data.OriginalMessageData = new OriginalEmailMessageData(message);
            Data.ScheduleMessageId = message.ScheduleMessageId == Guid.NewGuid() ? Data.Id : message.ScheduleMessageId;
            Data.TimeoutCounter = 0;
            var timeout = new DateTime(message.SendMessageAtUtc.Ticks, DateTimeKind.Utc);
            RequestUtcTimeout(timeout, new ScheduleEmailTimeout { TimeoutCounter = 0 });
            // TODO : Create handler for EmailScheduleCreated
            Bus.SendLocal(new EmailScheduleCreated
            {
                ScheduleId = Data.ScheduleMessageId,
                ScheduleTimeUtc = message.SendMessageAtUtc,
                EmailData = message.EmailData,
                Topic = message.Topic,
                Tags = message.Tags
            });
            Bus.Publish(new EmailScheduled
            {
                ScheduleMessageId = Data.ScheduleMessageId,
                CoordinatorId = message.CorrelationId,
                EmailData = message.EmailData,
                Topic = message.Topic,
                Tags = message.Tags,
                ScheduleSendingTimeUtc = message.SendMessageAtUtc,
                Username = message.Username
            });

        }

        public void Timeout(ScheduleEmailTimeout state)
        {
            throw new NotImplementedException();
        }

        public void Handle(PauseScheduledMessageIndefinitely message)
        {
            throw new NotImplementedException();
        }

        public void Handle(ResumeScheduledMessageWithOffset message)
        {
            throw new NotImplementedException();
        }

        public void Handle(RescheduleScheduledMessageWithNewTime message)
        {
            throw new NotImplementedException();
        }

        public void Handle(EmailStatusUpdate message)
        {
            throw new NotImplementedException();
        }
    }

    public class ScheduleEmailTimeout
    {
        public int TimeoutCounter { get; set; }
    }

    public class ScheduledEmailData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
        public virtual Guid ScheduleMessageId { get; set; }
        public virtual bool SchedulingPaused { get; set; }
        public virtual DateTime? LastUpdateCommandRequestUtc { get; set; }
        public virtual int TimeoutCounter { get; set; }
        public virtual OriginalEmailMessageData OriginalMessageData { get; set; }
    }

    public class OriginalEmailMessageData
    {
        public OriginalEmailMessageData() {}

        public OriginalEmailMessageData(ScheduleEmailForSendingLater requestMessage)
        {
            RequestingCoordinatorId = requestMessage.CorrelationId;
            Username = requestMessage.Username;
            // TODO : Flatten EmailData to make it more friendly for azure storage
            EmailData = requestMessage.EmailData;
            Topic = requestMessage.Topic;
            Tags = requestMessage.Tags;
            ConfirmationEmail = requestMessage.ConfirmationEmail;
            OriginalRequestSendTime = requestMessage.SendMessageAtUtc;
        }

        public virtual Guid RequestingCoordinatorId { get; set; }
        public virtual string Username { get; set; }
        public virtual EmailData EmailData { get; set; }
        public virtual string Topic { get; set; }
        public virtual IList<string> Tags { get; set; }
        public virtual string ConfirmationEmail { get; set; }
        public virtual DateTime OriginalRequestSendTime { get; set; }
    }
}
