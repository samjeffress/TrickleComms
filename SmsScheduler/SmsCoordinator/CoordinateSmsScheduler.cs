using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;
using SmsMessages.Scheduling;
using SmsMessages.Tracking;

namespace SmsCoordinator
{
    public class CoordinateSmsScheduler : 
        Saga<CoordinateSmsSchedulingData>,
        IAmStartedByMessages<TrickleSmsOverTimePeriod>, 
        IAmStartedByMessages<TrickleSmsSpacedByTimePeriod>,
        IHandleMessages<SmsScheduled>,
        IHandleMessages<ScheduledSmsSent>,
        IHandleMessages<PauseTrickledMessagesIndefinitely>,
        IHandleMessages<ResumeTrickledMessages>,
        IHandleMessages<MessageSchedulePaused>,
        IHandleMessages<MessageRescheduled>
    {
        public ICalculateSmsTiming TimingManager { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<ScheduledSmsSent>(data => data.Id, message => message.CoordinatorId);
            ConfigureMapping<PauseTrickledMessagesIndefinitely>(data => data.Id, message => message.CoordinatorId);
            ConfigureMapping<SmsScheduled>(data => data.Id, message => message.CoordinatorId);
            ConfigureMapping<ResumeTrickledMessages>(data => data.Id, message => message.CoordinatorId);
            ConfigureMapping<MessageSchedulePaused>(data => data.Id, message => message.CoordinatorId);
            ConfigureMapping<MessageRescheduled>(data => data.Id, message => message.CoordinatorId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(TrickleSmsOverTimePeriod message)
        {
            var messageTiming = TimingManager.CalculateTiming(message.StartTime, message.Duration, message.Messages.Count);
            var messageList = new List<ScheduleSmsForSendingLater>();
            Data.ScheduledMessageStatus = new List<ScheduledMessageStatus>();
            for (int i = 0; i < message.Messages.Count; i++)
            {
                var smsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(messageTiming[i], smsData, message.MetaData);
                messageList.Add(smsForSendingLater);
                Data.MessagesScheduled++;
                Data.ScheduledMessageStatus.Add(new ScheduledMessageStatus(smsForSendingLater));
            }
            Bus.Send(messageList);
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Data.Id,
                ScheduledMessages = messageList.Select(m => new MessageSchedule { Number = m.SmsData.Mobile, ScheduledTime = m.SendMessageAt, ScheduleMessageId = m.ScheduleMessageId }).ToList()
            };
            Bus.Send(coordinatorCreated);
        }

        public void Handle(TrickleSmsSpacedByTimePeriod trickleMultipleMessages)
        {
            var messageList = new List<ScheduleSmsForSendingLater>();
            Data.ScheduledMessageStatus = new List<ScheduledMessageStatus>();
            for(int i = 0; i < trickleMultipleMessages.Messages.Count; i++)
            {
                var extraTime = TimeSpan.FromTicks(trickleMultipleMessages.TimeSpacing.Ticks*i);
                var smsData = new SmsData(trickleMultipleMessages.Messages[i].Mobile, trickleMultipleMessages.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(trickleMultipleMessages.StartTime.Add(extraTime), smsData, trickleMultipleMessages.MetaData)
                {
                    CorrelationId = Data.Id
                };
                messageList.Add(smsForSendingLater);
                Data.MessagesScheduled++;
                Data.ScheduledMessageStatus.Add(new ScheduledMessageStatus(smsForSendingLater));
            }
            Bus.Send(messageList);
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Data.Id,
                ScheduledMessages = messageList.Select(m => new MessageSchedule { Number = m.SmsData.Mobile, ScheduledTime = m.SendMessageAt, ScheduleMessageId = m.ScheduleMessageId }).ToList()
            };
            Bus.Send(coordinatorCreated);
        }

        public void Handle(PauseTrickledMessagesIndefinitely message)
        {
            var messagesToPause = Data.ScheduledMessageStatus
                .Where(s => s.MessageStatus == MessageStatus.Scheduled || s.MessageStatus == MessageStatus.WaitingForScheduling)
                .ToList()
                .Select(scheduledMessageStatuse => 
                    new PauseScheduledMessageIndefinitely(scheduledMessageStatuse.ScheduledSms.ScheduleMessageId))
                .ToList();
            Bus.Send(messagesToPause);
        }

        public void Handle(ResumeTrickledMessages trickleMultipleMessages)
        {
            var offset = trickleMultipleMessages.ResumeTime.Ticks - Data.OriginalScheduleStartTime.Ticks;
            var messagesToResume = Data.ScheduledMessageStatus
                .Where(s => s.MessageStatus == MessageStatus.Paused)
                .ToList()
                .Select(scheduledMessageStatuse => 
                    new ResumeScheduledMessageWithOffset(scheduledMessageStatuse.ScheduledSms.ScheduleMessageId, new TimeSpan(offset)))
                .ToList();
            Bus.Send(messagesToResume);
        }

        public void Handle(SmsScheduled smsScheduled)
        {
            var messageStatus = Data.ScheduledMessageStatus.FirstOrDefault(s => s.ScheduledSms.ScheduleMessageId == smsScheduled.ScheduleMessageId);
            if (messageStatus == null)
                throw new Exception("Cannot find message with id " + smsScheduled.ScheduleMessageId);
            if (messageStatus.MessageStatus == MessageStatus.Sent)
                throw new Exception("Message already sent.");
            messageStatus.MessageStatus = MessageStatus.Scheduled;
            Bus.Send(new CoordinatorMessageScheduled { CoordinatorId = Data.Id, ScheduleMessageId = smsScheduled.ScheduleMessageId, Number = messageStatus.ScheduledSms.SmsData.Mobile });
        }

        public void Handle(MessageSchedulePaused message)
        {
            var messageStatus = Data.ScheduledMessageStatus.Where(s => s.ScheduledSms.ScheduleMessageId == message.ScheduleId).Select(s => s).FirstOrDefault();
            if (messageStatus == null)
                throw new Exception("Could not find message " + message.ScheduleId + ".");
            if (messageStatus.MessageStatus == MessageStatus.Sent)
                throw new Exception("Scheduled message " + message.ScheduleId + " is already sent.");
            messageStatus.MessageStatus = MessageStatus.Paused;
            Bus.Send(new CoordinatorMessagePaused { CoordinatorId = Data.Id, ScheduleMessageId = message.ScheduleId });
        }

        public void Handle(MessageRescheduled message)
        {
            var messageStatus = Data.ScheduledMessageStatus.Where(s => s.ScheduledSms.ScheduleMessageId == message.ScheduleMessageId).Select(s => s).FirstOrDefault();
            if (messageStatus == null)
                throw new Exception("Could not find message " + message.ScheduleMessageId + ".");
            if (messageStatus.MessageStatus == MessageStatus.Sent)
                throw new Exception("Scheduled message " + message.ScheduleMessageId + " is already sent.");
            messageStatus.MessageStatus = MessageStatus.Scheduled;
            Bus.Send(new CoordinatorMessageResumed
                         {
                             ScheduleMessageId = message.ScheduleMessageId,
                             CoordinatorId = Data.Id,
                             Number = messageStatus.ScheduledSms.SmsData.Mobile,
                             RescheduledTime = message.RescheduledTime
                         });
        }

        public void Handle(ScheduledSmsSent smsSent)
        {
            Data.MessagesConfirmedSent++;

            var scheduledMessageStatus = Data.ScheduledMessageStatus.FirstOrDefault(s => s.ScheduledSms.ScheduleMessageId == smsSent.ScheduledSmsId);
            if (scheduledMessageStatus == null)
                throw new Exception("Can't find scheduled message");

            scheduledMessageStatus.MessageStatus = MessageStatus.Sent;

            Bus.Send(new CoordinatorMessageSent
            {
                CoordinatorId = Data.Id,
                ScheduleMessageId = smsSent.ScheduledSmsId,
                Cost = smsSent.ConfirmationData.Price,
                TimeSent = smsSent.ConfirmationData.SentAt,
                Number = smsSent.Number
            });

            if (Data.MessagesScheduled == Data.MessagesConfirmedSent)
                MarkAsComplete();
        }
    }

    public class CoordinateSmsSchedulingData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public int MessagesScheduled { get; set; }
        public int MessagesConfirmedSent { get; set; }

        public List<ScheduledMessageStatus> ScheduledMessageStatus { get; set; }

        public DateTime OriginalScheduleStartTime { get; set; }
    }

    public class ScheduledMessageStatus
    {
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