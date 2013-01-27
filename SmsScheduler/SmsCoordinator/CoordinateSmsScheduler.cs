using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsMessages.Coordinator.Events;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;
using SmsTrackingMessages.Messages;

namespace SmsCoordinator
{
    public class CoordinateSmsScheduler : 
        Saga<CoordinateSmsSchedulingData>,
        IAmStartedByMessages<TrickleSmsOverCalculatedIntervalsBetweenSetDates>, 
        IAmStartedByMessages<TrickleSmsWithDefinedTimeBetweenEachMessage>,
        IHandleMessages<SmsScheduled>,
        IHandleMessages<ScheduledSmsSent>,
        IHandleMessages<ScheduledSmsFailed>,
        IHandleMessages<PauseTrickledMessagesIndefinitely>,
        IHandleMessages<ResumeTrickledMessages>,
        IHandleMessages<MessageSchedulePaused>,
        IHandleMessages<MessageRescheduled>
    {
        public ICalculateSmsTiming TimingManager { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<ScheduledSmsSent>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<ScheduledSmsFailed>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<PauseTrickledMessagesIndefinitely>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<SmsScheduled>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<ResumeTrickledMessages>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<MessageSchedulePaused>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<MessageRescheduled>(data => data.CoordinatorId, message => message.CoordinatorId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(TrickleSmsOverCalculatedIntervalsBetweenSetDates message)
        {
            Data.CoordinatorId = message.CoordinatorId == Guid.Empty ? Data.Id : message.CoordinatorId;
            Data.OriginalScheduleStartTime = message.StartTimeUtc;
            var messageTiming = TimingManager.CalculateTiming(message.StartTimeUtc, message.Duration, message.Messages.Count);
            var messageList = new List<ScheduleSmsForSendingLater>();
            Data.ScheduledMessageStatus = new List<ScheduledMessageStatus>();
            for (int i = 0; i < message.Messages.Count; i++)
            {
                var smsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(messageTiming[i].ToUniversalTime(), smsData, message.MetaData, Data.CoordinatorId);
                messageList.Add(smsForSendingLater);
                Data.MessagesScheduled++;
                Data.ScheduledMessageStatus.Add(new ScheduledMessageStatus(smsForSendingLater));
            }
            Bus.Send(messageList.ToArray());
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Data.CoordinatorId,
                ScheduledMessages = messageList.Select(m => new MessageSchedule { Number = m.SmsData.Mobile, ScheduledTimeUtc = m.SendMessageAtUtc, ScheduleMessageId = m.ScheduleMessageId }).ToList(),
                CreationDateUtc = DateTime.UtcNow,
                MetaData = message.MetaData,
                ConfirmationEmailAddress = message.ConfirmationEmail
            };
            Bus.Send(coordinatorCreated);
        }

        public void Handle(TrickleSmsWithDefinedTimeBetweenEachMessage message)
        {
            Data.CoordinatorId = message.CoordinatorId == Guid.Empty ? Data.Id : message.CoordinatorId;
            Data.OriginalScheduleStartTime = message.StartTimeUtc;
            var messageList = new List<ScheduleSmsForSendingLater>();
            Data.ScheduledMessageStatus = new List<ScheduledMessageStatus>();
            for(int i = 0; i < message.Messages.Count; i++)
            {
                var extraTime = TimeSpan.FromTicks(message.TimeSpacing.Ticks*i);
                var smsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(message.StartTimeUtc.Add(extraTime), smsData, message.MetaData, Data.CoordinatorId)
                {
                    CorrelationId = Data.CoordinatorId
                };
                messageList.Add(smsForSendingLater);
                Data.MessagesScheduled++;
                Data.ScheduledMessageStatus.Add(new ScheduledMessageStatus(smsForSendingLater));
            }
            Bus.Send(messageList.ToArray());
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Data.CoordinatorId,
                ScheduledMessages = messageList.Select(m => new MessageSchedule { Number = m.SmsData.Mobile, ScheduledTimeUtc = m.SendMessageAtUtc, ScheduleMessageId = m.ScheduleMessageId }).ToList(),
                CreationDateUtc = DateTime.UtcNow,
                MetaData = message.MetaData,
                ConfirmationEmailAddress = message.ConfirmationEmail
            };
            Bus.Send(coordinatorCreated);
        }

        public void Handle(PauseTrickledMessagesIndefinitely message)
        {
            if (Data.LastUpdatingCommandRequestUtc != null && Data.LastUpdatingCommandRequestUtc < message.MessageRequestTimeUtc)
                return;
            var messagesToPause = Data.ScheduledMessageStatus
                .Where(s => s.MessageStatus == MessageStatus.Scheduled || s.MessageStatus == MessageStatus.WaitingForScheduling)
                .ToList()
                .Select(scheduledMessageStatuse => 
                    new PauseScheduledMessageIndefinitely(scheduledMessageStatuse.ScheduledSms.ScheduleMessageId))
                .ToList();
            foreach (var pauseScheduledMessageIndefinitely in messagesToPause)
            {
                Bus.Send(pauseScheduledMessageIndefinitely);
            }
            Data.LastUpdatingCommandRequestUtc = message.MessageRequestTimeUtc;
        }

        public void Handle(ResumeTrickledMessages resumeMessages)
        {
            if (Data.LastUpdatingCommandRequestUtc != null && Data.LastUpdatingCommandRequestUtc < resumeMessages.MessageRequestTimeUtc)
                return;
            var offset = resumeMessages.ResumeTimeUtc.Ticks - Data.OriginalScheduleStartTime.Ticks;
            var resumeMessageCommands = Data.ScheduledMessageStatus
                .Where(s => s.MessageStatus == MessageStatus.Paused)
                .ToList()
                .Select(scheduledMessageStatuse => 
                    new ResumeScheduledMessageWithOffset(scheduledMessageStatuse.ScheduledSms.ScheduleMessageId, new TimeSpan(offset)))
                .ToList();
            foreach (var resumeScheduledMessageWithOffset in resumeMessageCommands)
            {
                Bus.Send(resumeScheduledMessageWithOffset);
            }
            Data.LastUpdatingCommandRequestUtc = resumeMessages.MessageRequestTimeUtc;
        }

        public void Handle(SmsScheduled smsScheduled)
        {
            var messageStatus = Data.ScheduledMessageStatus.FirstOrDefault(s => s.ScheduledSms.ScheduleMessageId == smsScheduled.ScheduleMessageId);
            if (messageStatus == null)
                throw new Exception("Cannot find message with id " + smsScheduled.ScheduleMessageId);
            if (messageStatus.MessageStatus == MessageStatus.Sent)
                throw new Exception("Message already sent.");
            messageStatus.MessageStatus = MessageStatus.Scheduled;
        }

        public void Handle(MessageSchedulePaused message)
        {
            var messageStatus = Data.ScheduledMessageStatus.Where(s => s.ScheduledSms.ScheduleMessageId == message.ScheduleId).Select(s => s).FirstOrDefault();
            if (messageStatus == null)
                throw new Exception("Could not find message " + message.ScheduleId + ".");
            if (messageStatus.MessageStatus == MessageStatus.Sent)
                throw new Exception("Scheduled message " + message.ScheduleId + " is already sent.");
            messageStatus.MessageStatus = MessageStatus.Paused;
        }

        public void Handle(MessageRescheduled message)
        {
            var messageStatus = Data.ScheduledMessageStatus.Where(s => s.ScheduledSms.ScheduleMessageId == message.ScheduleMessageId).Select(s => s).FirstOrDefault();
            if (messageStatus == null)
                throw new Exception("Could not find message " + message.ScheduleMessageId + ".");
            if (messageStatus.MessageStatus == MessageStatus.Sent)
                throw new Exception("Scheduled message " + message.ScheduleMessageId + " is already sent.");
            messageStatus.MessageStatus = MessageStatus.Scheduled;
        }

        public void Handle(ScheduledSmsSent smsSent)
        {
            Data.MessagesConfirmedSentOrFailed++;

            var scheduledMessageStatus = Data.ScheduledMessageStatus.FirstOrDefault(s => s.ScheduledSms.ScheduleMessageId == smsSent.ScheduledSmsId);
            if (scheduledMessageStatus == null)
                throw new Exception("Can't find scheduled message");

            scheduledMessageStatus.MessageStatus = MessageStatus.Sent;

            if (Data.MessagesScheduled == Data.MessagesConfirmedSentOrFailed)
            {
                //Bus.Send(new CoordinatorCompleted { CoordinatorId = Data.CoordinatorId, CompletionDate = DateTime.UtcNow });
                Bus.Publish(new SmsMessages.Coordinator.Events.CoordinatorCompleted { CoordinatorId = Data.CoordinatorId, CompletionDateUtc = DateTime.UtcNow });
                MarkAsComplete();
            }
        }

        public void Handle(ScheduledSmsFailed failureMessage)
        {
            Data.MessagesConfirmedSentOrFailed++;

            var scheduledMessageStatus = Data.ScheduledMessageStatus.FirstOrDefault(s => s.ScheduledSms.ScheduleMessageId == failureMessage.ScheduledSmsId);
            if (scheduledMessageStatus == null)
                throw new Exception("Can't find scheduled message");

            scheduledMessageStatus.MessageStatus = MessageStatus.Failed;

            if (Data.MessagesScheduled == Data.MessagesConfirmedSentOrFailed)
            {
                Bus.Send(new CoordinatorCompleted { CoordinatorId = Data.CoordinatorId, CompletionDateUtc = DateTime.UtcNow });
                Bus.Publish(new CoordinatorCompleted { CoordinatorId = Data.CoordinatorId, CompletionDateUtc = DateTime.UtcNow });
                MarkAsComplete();
            }
        }
    }

    public class CoordinateSmsSchedulingData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public int MessagesScheduled { get; set; }
        public int MessagesConfirmedSentOrFailed { get; set; }

        public List<ScheduledMessageStatus> ScheduledMessageStatus { get; set; }

        public DateTime OriginalScheduleStartTime { get; set; }

        public Guid CoordinatorId { get; set; }

        public DateTime? LastUpdatingCommandRequestUtc { get; set; }
    }

    public class ScheduledMessageStatus
    {
        [Obsolete("For JSON deserialisation.")]
        public ScheduledMessageStatus() {}

        public ScheduledMessageStatus(ScheduleSmsForSendingLater message)
        {
            MessageStatus = MessageStatus.WaitingForScheduling;
            ScheduledSms = message;
        }

        public ScheduledMessageStatus(ScheduleSmsForSendingLater message, MessageStatus status)
        {
            MessageStatus = status;
            ScheduledSms = message;
        }

        public MessageStatus MessageStatus { get; set; }

        public ScheduleSmsForSendingLater ScheduledSms { get; set; }
    }
}