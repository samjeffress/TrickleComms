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

namespace SmsCoordinator
{
    public class CoordinateSmsScheduler : 
        Saga<CoordinateSmsSchedulingData>,
        IAmStartedByMessages<TrickleSmsOverCalculatedIntervalsBetweenSetDates>, 
        IAmStartedByMessages<TrickleSmsWithDefinedTimeBetweenEachMessage>,
        IAmStartedByMessages<SendAllMessagesAtOnce>,
        IHandleMessages<ScheduledSmsSent>,
        IHandleMessages<ScheduledSmsFailed>,
        IHandleMessages<PauseTrickledMessagesIndefinitely>,
        IHandleMessages<ResumeTrickledMessages>,
        IHandleMessages<RescheduleTrickledMessages>
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
            ConfigureMapping<RescheduleTrickledMessages>(data => data.CoordinatorId, message => message.CoordinatorId);
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
                var smsForSendingLater = new ScheduleSmsForSendingLater(messageTiming[i], smsData, message.MetaData, Data.CoordinatorId);
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
                ConfirmationEmailAddresses = message.ConfirmationEmails,
                UserOlsenTimeZone = message.UserOlsenTimeZone
            };
            Bus.Publish(coordinatorCreated);
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
                ConfirmationEmailAddresses = message.ConfirmationEmails,
                UserOlsenTimeZone = message.UserOlsenTimeZone
            };
            Bus.Publish(coordinatorCreated);
        }

        public void Handle(SendAllMessagesAtOnce message)
        {
            Data.CoordinatorId = message.CoordinatorId == Guid.Empty ? Data.Id : message.CoordinatorId;
            Data.OriginalScheduleStartTime = message.SendTimeUtc;
            var messageList = new List<ScheduleSmsForSendingLater>();
            Data.ScheduledMessageStatus = new List<ScheduledMessageStatus>();
            for (int i = 0; i < message.Messages.Count; i++)
            {
                var smsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(message.SendTimeUtc, smsData, message.MetaData, Data.CoordinatorId)
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
                ConfirmationEmailAddresses = message.ConfirmationEmails,
                UserOlsenTimeZone = message.UserOlsenTimeZone
            };
            Bus.Publish(coordinatorCreated);
        }

        public void Handle(PauseTrickledMessagesIndefinitely message)
        {
            if (Data.LastUpdatingCommandRequestUtc != null && Data.LastUpdatingCommandRequestUtc > message.MessageRequestTimeUtc)
                return;
            var messagesToPause = Data.ScheduledMessageStatus
                .Where(s => s.Status == ScheduleStatus.Initiated)
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
            if (Data.LastUpdatingCommandRequestUtc != null && Data.LastUpdatingCommandRequestUtc > resumeMessages.MessageRequestTimeUtc)
                return;
            var offset = resumeMessages.ResumeTimeUtc.Ticks - Data.OriginalScheduleStartTime.Ticks;
            var resumeMessageCommands = Data.ScheduledMessageStatus
                .Where(s => s.Status == ScheduleStatus.Initiated)
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

        public void Handle(RescheduleTrickledMessages rescheduleTrickledMessages)
        {
            if (Data.LastUpdatingCommandRequestUtc != null && Data.LastUpdatingCommandRequestUtc > rescheduleTrickledMessages.MessageRequestTimeUtc)
                return;
//            var baseOffset = rescheduleTrickledMessages.ResumeTimeUtc.Ticks - Data.OriginalScheduleStartTime.Ticks;
            var activeMessageStatuses = Data.ScheduledMessageStatus
                .Where(s => s.Status == ScheduleStatus.Initiated)
                .ToList();
            var messageResumeSpan = (rescheduleTrickledMessages.FinishTimeUtc.Ticks - rescheduleTrickledMessages.ResumeTimeUtc.Ticks);
            long messageOffset = 0;
            if (activeMessageStatuses.Count > 1)
                messageOffset = messageResumeSpan/(activeMessageStatuses.Count - 1);

            for (var i = 0; i < activeMessageStatuses.Count; i++)
            {
                var resumeScheduledMessageWithOffset = new RescheduleScheduledMessageWithNewTime(activeMessageStatuses[i].ScheduledSms.ScheduleMessageId, new DateTime(rescheduleTrickledMessages.ResumeTimeUtc.Ticks + (i*messageOffset), DateTimeKind.Utc));
                Bus.Send(resumeScheduledMessageWithOffset);
            }

            Data.LastUpdatingCommandRequestUtc = rescheduleTrickledMessages.MessageRequestTimeUtc;
        }

        //public void Handle(SmsScheduled smsScheduled)
        //{
        //    var messageStatus = Data.ScheduledMessageStatus.FirstOrDefault(s => s.ScheduledSms.ScheduleMessageId == smsScheduled.ScheduleMessageId);
        //    if (messageStatus == null)
        //        throw new Exception("Cannot find message with id " + smsScheduled.ScheduleMessageId);
        //    if (messageStatus.MessageStatus == MessageStatus.Sent)
        //        throw new Exception("Message already sent.");
        //    messageStatus.MessageStatus = MessageStatus.Scheduled;
        //}

        //public void Handle(MessageSchedulePaused message)
        //{
        //    var messageStatus = Data.ScheduledMessageStatus.Where(s => s.ScheduledSms.ScheduleMessageId == message.ScheduleId).Select(s => s).FirstOrDefault();
        //    if (messageStatus == null)
        //        throw new Exception("Could not find message " + message.ScheduleId + ".");
        //    if (messageStatus.MessageStatus == MessageStatus.Sent)
        //        throw new Exception("Scheduled message " + message.ScheduleId + " is already sent.");
        //    messageStatus.MessageStatus = MessageStatus.Paused;
        //}

        //public void Handle(MessageRescheduled message)
        //{
        //    var messageStatus = Data.ScheduledMessageStatus.Where(s => s.ScheduledSms.ScheduleMessageId == message.ScheduleMessageId).Select(s => s).FirstOrDefault();
        //    if (messageStatus == null)
        //        throw new Exception("Could not find message " + message.ScheduleMessageId + ".");
        //    if (messageStatus.MessageStatus == MessageStatus.Sent)
        //        throw new Exception("Scheduled message " + message.ScheduleMessageId + " is already sent.");
        //    messageStatus.MessageStatus = MessageStatus.Scheduled;
        //}

        public void Handle(ScheduledSmsSent smsSent)
        {
            Data.MessagesConfirmedSentOrFailed++;

            var scheduledMessageStatus = Data.ScheduledMessageStatus.FirstOrDefault(s => s.ScheduledSms.ScheduleMessageId == smsSent.ScheduledSmsId);
            if (scheduledMessageStatus == null)
                throw new Exception("Can't find scheduled message");

            scheduledMessageStatus.MessageStatus = MessageStatus.Sent;
            scheduledMessageStatus.Status = ScheduleStatus.Sent;

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
            scheduledMessageStatus.Status = ScheduleStatus.Failed;
            
            if (Data.MessagesScheduled == Data.MessagesConfirmedSentOrFailed)
            {
                //Bus.Send(new CoordinatorCompleted { CoordinatorId = Data.CoordinatorId, CompletionDateUtc = DateTime.UtcNow });
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
        public ScheduledMessageStatus() { }

        public ScheduledMessageStatus(ScheduleSmsForSendingLater message)
        {
            MessageStatus = MessageStatus.WaitingForScheduling;
            Status = ScheduleStatus.Initiated;
            ScheduledSms = message;
        }

        //public ScheduledMessageStatus(ScheduleSmsForSendingLater message, ScheduleStatus status)
        //{
        //    //MessageStatus = status;
        //    Status = status;
        //    ScheduledSms = message;
        //}

        [Obsolete("Using internal enum - not interested in Scheduled, Paused etc - only Sent, Failed, Cancelled, Initiated")]
        public MessageStatus MessageStatus { get; set; }

        public ScheduleStatus Status { get; set; }

        public ScheduleSmsForSendingLater ScheduledSms { get; set; }
    }
    public enum ScheduleStatus
    {
        Initiated,
        Sent,
        Failed,
        Cancelled
    }
}