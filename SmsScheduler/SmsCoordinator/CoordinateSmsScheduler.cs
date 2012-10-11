using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.Commands;
using SmsMessages.CommonData;

namespace SmsCoordinator
{
    public class CoordinateSmsScheduler : 
        Saga<CoordinateSmsSchedulingData>,
        IAmStartedByMessages<TrickleSmsOverTimePeriod>, 
        IAmStartedByMessages<TrickleSmsSpacedByTimePeriod>,
        IHandleMessages<ScheduledSmsSent>,
        IHandleMessages<PauseTrickledMessagesIndefinitely>,
        IHandleMessages<ResumeTrickledMessagesNow>
    {
        public ICalculateSmsTiming TimingManager { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<ScheduledSmsSent>(data => data.Id, message => message.CoordinatorId);
            ConfigureMapping<PauseTrickledMessagesIndefinitely>(data => data.Id, message => message.CoordinatorId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(TrickleSmsOverTimePeriod message)
        {
            var messageTiming = TimingManager.CalculateTiming(message.StartTime, message.Duration, message.Messages.Count);
            var messageList = new List<ScheduleSmsForSendingLater>();
            Data.ScheduledMessageStatus = new List<ScheduledMessageStatus>();
            for (int i = 0; i < message.Messages.Count; i++)
            {
                var smsForSendingLater = new ScheduleSmsForSendingLater
                {
                    SendMessageAt = messageTiming[i],
                    SmsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message),
                    SmsMetaData = message.MetaData
                };
                messageList.Add(smsForSendingLater);
                Data.MessagesScheduled++;
                Data.ScheduledMessageStatus.Add(new ScheduledMessageStatus(smsForSendingLater));
            }
            Bus.Send(messageList);
        }

        public void Handle(TrickleSmsSpacedByTimePeriod trickleMultipleMessages)
        {
            var messageList = new List<ScheduleSmsForSendingLater>();
            Data.ScheduledMessageStatus = new List<ScheduledMessageStatus>();
            for(int i = 0; i < trickleMultipleMessages.Messages.Count; i++)
            {
                var extraTime = TimeSpan.FromTicks(trickleMultipleMessages.TimeSpacing.Ticks*i);
                var smsForSendingLater = new ScheduleSmsForSendingLater
                {
                    SendMessageAt = trickleMultipleMessages.StartTime.Add(extraTime),
                    SmsData = new SmsData(trickleMultipleMessages.Messages[i].Mobile, trickleMultipleMessages.Messages[i].Message),
                    SmsMetaData = trickleMultipleMessages.MetaData
                };
                messageList.Add(smsForSendingLater);
                Data.MessagesScheduled++;
                Data.ScheduledMessageStatus.Add(new ScheduledMessageStatus(smsForSendingLater));
            }
            Bus.Send(messageList);
        }

        public void Handle(ScheduledSmsSent smsSent)
        {
            Data.MessagesConfirmedSent++;
            if (Data.MessagesScheduled == Data.MessagesConfirmedSent)
                MarkAsComplete();
        }

        public void Handle(PauseTrickledMessagesIndefinitely message)
        {
            var messagesToPause = new List<PauseScheduledMessageIndefinitely>();
            foreach (var scheduledMessageStatuse in Data.ScheduledMessageStatus.Where(s => s.MessageStatus == MessageStatus.Scheduled).ToList())
            {
                messagesToPause.Add(new PauseScheduledMessageIndefinitely (scheduledMessageStatuse.ScheduledSms.ScheduleMessageId));
                scheduledMessageStatuse.MessageStatus = MessageStatus.Paused;
            }
            Bus.Send(messagesToPause);
        }

        public void Handle(ResumeTrickledMessagesNow trickleMultipleMessages)
        {
            var offset = trickleMultipleMessages.ResumeTime.Ticks - Data.OriginalScheduleStartTime.Ticks;
            var messagesToResume = new List<ResumeScheduledMessageWithOffset>();
            foreach (var scheduledMessageStatuse in Data.ScheduledMessageStatus.Where(s => s.MessageStatus == MessageStatus.Scheduled).ToList())
            {
                messagesToResume.Add(new ResumeScheduledMessageWithOffset(scheduledMessageStatuse.ScheduledSms.ScheduleMessageId, new TimeSpan(offset)));
                scheduledMessageStatuse.MessageStatus = MessageStatus.Scheduled;
            }
            Bus.Send(messagesToResume);
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
            MessageStatus = MessageStatus.Scheduled;
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

    public enum MessageStatus
    {
        Scheduled,
        Sent,
        Paused,
        Cancelled
    }
}