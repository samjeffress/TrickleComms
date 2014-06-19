using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
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
        IHandleMessages<EmailStatusUpdate>,
        IHandleTimeouts<ScheduleEmailDeliveredTimeout>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<EmailStatusUpdate>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<PauseScheduledMessageIndefinitely>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            ConfigureMapping<ResumeScheduledMessageWithOffset>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            ConfigureMapping<RescheduleScheduledMessageWithNewTime>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(ScheduleEmailForSendingLater message)
        {
            Data.OriginalMessageData = new OriginalEmailMessageData(message);
            Data.ScheduleMessageId = message.ScheduleMessageId == Guid.NewGuid() ? Data.Id : message.ScheduleMessageId;
            Data.TimeoutCounter = 0;
            var timeout = new DateTime(message.SendMessageAtUtc.Ticks, DateTimeKind.Utc);
            RequestUtcTimeout(timeout, new ScheduleEmailTimeout { TimeoutCounter = 0 });
            // TODO: Save coordinator id
            Bus.SendLocal(new EmailScheduleCreated
            {
                CorrelationId = message.CorrelationId,
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
            if (!Data.SchedulingPaused && state.TimeoutCounter == Data.TimeoutCounter)
            {
                var originalMessage = Data.OriginalMessageData;
                var sendOneEmailNow = new SendOneEmailNow
                {
                    CorrelationId = Data.Id,
                    BodyHtml = Data.OriginalMessageData.BodyHtml,
                    BodyText = Data.OriginalMessageData.BodyText,
                    FromAddress = Data.OriginalMessageData.FromAddress,
                    FromDisplayName = Data.OriginalMessageData.FromDisplayName,
                    ReplyToAddress = Data.OriginalMessageData.ReplyToAddress,
                    Subject = Data.OriginalMessageData.Subject,
                    ToAddress = Data.OriginalMessageData.ToAddress,
                    Username = originalMessage.Username
                };
                Bus.Send("smsactioner", sendOneEmailNow);
            }
        }

        public void Handle(PauseScheduledMessageIndefinitely message)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > message.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = true;
            Bus.SendLocal(new ScheduleStatusChanged
            {
                ScheduleId = message.ScheduleMessageId,
                RequestTimeUtc = message.MessageRequestTimeUtc,
                Status = MessageStatus.Paused
            });
            var originalMessage = Data.OriginalMessageData;
            Bus.Publish(new MessageSchedulePaused
            {
                CoordinatorId = originalMessage.RequestingCoordinatorId,
                ScheduleId = message.ScheduleMessageId,
                // TODO : Should we remove the number or have a different thing for emails?
            });
            Data.LastUpdateCommandRequestUtc = message.MessageRequestTimeUtc;
        }

        public void Handle(ResumeScheduledMessageWithOffset message)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > message.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = false;
            var rescheduledTime = Data.OriginalMessageData.OriginalRequestSendTime.Add(message.Offset);
            Data.TimeoutCounter++;
            RequestUtcTimeout(rescheduledTime, new ScheduleEmailTimeout { TimeoutCounter = Data.TimeoutCounter });
            Bus.Publish(new MessageRescheduled
            {
                CoordinatorId = Data.OriginalMessageData.RequestingCoordinatorId,
                ScheduleMessageId = Data.ScheduleMessageId,
                RescheduledTimeUtc = rescheduledTime,
                // TODO : Should we remove the number or have a different thing for emails?
            });
            Bus.SendLocal(new ScheduleStatusChanged
            {
                RequestTimeUtc = message.MessageRequestTimeUtc,
                ScheduleId = message.ScheduleMessageId,
                ScheduleTimeUtc = rescheduledTime,
                Status = MessageStatus.Scheduled
            });
            Data.LastUpdateCommandRequestUtc = message.MessageRequestTimeUtc;
        }

        public void Handle(RescheduleScheduledMessageWithNewTime message)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > message.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = false;
            Data.TimeoutCounter++;
            RequestUtcTimeout(message.NewScheduleTimeUtc, new ScheduleEmailTimeout { TimeoutCounter = Data.TimeoutCounter });
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
                // TODO : Should we remove the number or have a different thing for emails?
            });
            Data.LastUpdateCommandRequestUtc = message.MessageRequestTimeUtc;
        }

        public void Handle(EmailStatusUpdate message)
        {
            if (message.Status == EmailStatus.Opened || message.Status == EmailStatus.Clicked)
            {
                var scheduledEmailSent = new ScheduledEmailSent
                    {
                        CoordinatorId = Data.OriginalMessageData.RequestingCoordinatorId,
                        EmailStatus = message.Status,
                        ScheduledSmsId = Data.ScheduleMessageId,
                        ToAddress = Data.OriginalMessageData.ToAddress,
                        Username = Data.OriginalMessageData.Username
                    };
                Bus.Publish(scheduledEmailSent);
                Bus.SendLocal(new ScheduleStatusChanged
                    {
                        ScheduleId = Data.ScheduleMessageId,
                        Status = MessageStatus.Sent
                    });
                MarkAsComplete();
                return;
            }
            if (message.Status == EmailStatus.Failed || message.Status == EmailStatus.Rejected 
                || message.Status == EmailStatus.Unsubscribed || message.Status == EmailStatus.Complained)
            {
                var scheduledEmailFailed = new ScheduledEmailFailed
                {
                    CoordinatorId = Data.OriginalMessageData.RequestingCoordinatorId,
                    EmailStatus = message.Status,
                    ScheduledSmsId = Data.ScheduleMessageId,
                    ToAddress = Data.OriginalMessageData.ToAddress,
                    Username = Data.OriginalMessageData.Username
                };
                Bus.Publish(scheduledEmailFailed);
                Bus.SendLocal(new ScheduleStatusChanged
                {
                    ScheduleId = Data.ScheduleMessageId,
                    Status = MessageStatus.Failed
                });
                MarkAsComplete();
                return;
            }
            
            if (message.Status == EmailStatus.Delivered)
            {
                RequestUtcTimeout<ScheduleEmailDeliveredTimeout>(new TimeSpan(1, 0, 0, 0));
            }
        }

        public void Timeout(ScheduleEmailDeliveredTimeout state)
        {
            // Haven't heard anything more about the email so we presume that it was successful
            var scheduledEmailSent = new ScheduledEmailSent
            {
                CoordinatorId = Data.OriginalMessageData.RequestingCoordinatorId,
                EmailStatus = EmailStatus.Delivered,
                ScheduledSmsId = Data.ScheduleMessageId,
                ToAddress = Data.OriginalMessageData.ToAddress,
                Username = Data.OriginalMessageData.Username
            };
            Bus.Publish(scheduledEmailSent);
            Bus.SendLocal(new ScheduleStatusChanged
            {
                ScheduleId = Data.ScheduleMessageId,
                Status = MessageStatus.Sent
            });
            MarkAsComplete();
        }
    }

    public class ScheduleEmailTimeout
    {
        public int TimeoutCounter { get; set; }
    }

    public class ScheduleEmailDeliveredTimeout
    {}

    public class ScheduledEmailData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }

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
            ToAddress = requestMessage.EmailData.ToAddress;
            FromAddress = requestMessage.EmailData.FromAddress;
            FromDisplayName = requestMessage.EmailData.FromDisplayName;
            ReplyToAddress = requestMessage.EmailData.ReplyToAddress;
            Subject = requestMessage.EmailData.Subject;
            BodyHtml = requestMessage.EmailData.BodyHtml;
            BodyText = requestMessage.EmailData.BodyText;
            Topic = requestMessage.Topic;
            Tags = requestMessage.Tags;
            ConfirmationEmail = requestMessage.ConfirmationEmail;
            OriginalRequestSendTime = requestMessage.SendMessageAtUtc;
        }

        public virtual Guid RequestingCoordinatorId { get; set; }
        public virtual string Username { get; set; }
        public virtual string Topic { get; set; }
        public virtual IList<string> Tags { get; set; }
        public virtual string ConfirmationEmail { get; set; }
        public virtual DateTime OriginalRequestSendTime { get; set; }
        public virtual string ToAddress { get; set; }
        public virtual string FromAddress { get; set; }
        public virtual string FromDisplayName { get; set; }
        public virtual string ReplyToAddress { get; set; }
        public virtual string Subject { get; set; }
        public virtual string BodyHtml { get; set; }
        public virtual string BodyText { get; set; }
    }
}
