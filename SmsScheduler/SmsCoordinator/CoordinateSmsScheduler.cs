using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsMessages.Coordinator.Events;
using SmsMessages.Email.Commands;
using SmsMessages.Scheduling.Commands;

namespace SmsCoordinator
{
    public class CoordinateSmsScheduler : 
        Saga<CoordinateSmsSchedulingData>,
        IAmStartedByMessages<TrickleSmsOverCalculatedIntervalsBetweenSetDates>, 
        IAmStartedByMessages<TrickleSmsWithDefinedTimeBetweenEachMessage>,
        IAmStartedByMessages<SendAllMessagesAtOnce>,
        IHandleMessages<PauseTrickledMessagesIndefinitely>,
        IHandleMessages<ResumeTrickledMessages>,
        IHandleMessages<RescheduleTrickledMessages>,
        IHandleTimeouts<CoordinatorTimeout>
    {
        public ICalculateSmsTiming TimingManager { get; set; }

        public IRavenScheduleDocuments RavenScheduleDocuments { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<PauseTrickledMessagesIndefinitely>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<ResumeTrickledMessages>(data => data.CoordinatorId, message => message.CoordinatorId);
            ConfigureMapping<RescheduleTrickledMessages>(data => data.CoordinatorId, message => message.CoordinatorId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(TrickleSmsOverCalculatedIntervalsBetweenSetDates message)
        {
            Data.CoordinatorId = message.CoordinatorId == Guid.Empty ? Data.Id : message.CoordinatorId;
            Data.OriginalScheduleStartTime = message.StartTimeUtc;
            Data.EmailAddresses = message.ConfirmationEmails;
            Data.UserOlsenTimeZone = message.UserOlsenTimeZone;
            Data.Topic = message.MetaData.Topic;
            var messageTiming = TimingManager.CalculateTiming(message.StartTimeUtc, message.Duration, message.Messages.Count);
            var lastScheduledMessageTime = message.StartTimeUtc.Add(message.Duration);
            var messageList = new List<ScheduleSmsForSendingLater>();
            for (int i = 0; i < message.Messages.Count; i++)
            {
                var smsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(messageTiming[i], smsData, message.MetaData, Data.CoordinatorId);
                messageList.Add(smsForSendingLater);
            }
            Bus.Send(messageList.ToArray());
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Data.CoordinatorId,
                ScheduledMessages = messageList.Select(m => new MessageSchedule { Number = m.SmsData.Mobile, ScheduledTimeUtc = m.SendMessageAtUtc, ScheduleMessageId = m.ScheduleMessageId }).ToList(),
                CreationDateUtc = DateTime.UtcNow,
                MetaData = message.MetaData,
                ConfirmationEmailAddresses = message.ConfirmationEmails,
                UserOlsenTimeZone = message.UserOlsenTimeZone,
                MessageBody = message.Messages.First().Message,
                MessageCount = message.Messages.Count
            };

            RavenScheduleDocuments.SaveCoordinator(coordinatorCreated);
            RavenScheduleDocuments.SaveSchedules(messageList, Data.CoordinatorId);

            RequestUtcTimeout<CoordinatorTimeout>(lastScheduledMessageTime.AddMinutes(2));
            Bus.Publish(coordinatorCreated);
            Bus.SendLocal(new CoordinatorCreatedEmail(coordinatorCreated));
        }

        public void Handle(TrickleSmsWithDefinedTimeBetweenEachMessage message)
        {
            Data.CoordinatorId = message.CoordinatorId == Guid.Empty ? Data.Id : message.CoordinatorId;
            Data.OriginalScheduleStartTime = message.StartTimeUtc;
            Data.EmailAddresses = message.ConfirmationEmails;
            Data.UserOlsenTimeZone = message.UserOlsenTimeZone;
            Data.Topic = message.MetaData.Topic;
            var messageList = new List<ScheduleSmsForSendingLater>();
            DateTime lastScheduledMessageTime = DateTime.Now;
            for(int i = 0; i < message.Messages.Count; i++)
            {
                var extraTime = TimeSpan.FromTicks(message.TimeSpacing.Ticks*i);
                lastScheduledMessageTime = message.StartTimeUtc.Add(extraTime);
                var smsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(message.StartTimeUtc.Add(extraTime), smsData, message.MetaData, Data.CoordinatorId)
                {
                    CorrelationId = Data.CoordinatorId
                };
                messageList.Add(smsForSendingLater);
            }
            Bus.Send(messageList.ToArray());
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Data.CoordinatorId,
                ScheduledMessages = messageList.Select(m => new MessageSchedule { Number = m.SmsData.Mobile, ScheduledTimeUtc = m.SendMessageAtUtc, ScheduleMessageId = m.ScheduleMessageId }).ToList(),
                CreationDateUtc = DateTime.UtcNow,
                MetaData = message.MetaData,
                ConfirmationEmailAddresses = message.ConfirmationEmails,
                UserOlsenTimeZone = message.UserOlsenTimeZone,
                MessageBody = message.Messages.First().Message,
                MessageCount = message.Messages.Count
            };
            Bus.Publish(coordinatorCreated);
            Bus.SendLocal(new CoordinatorCreatedEmail(coordinatorCreated));
            RequestUtcTimeout<CoordinatorTimeout>(lastScheduledMessageTime.AddMinutes(2));

            RavenScheduleDocuments.SaveCoordinator(coordinatorCreated);
            RavenScheduleDocuments.SaveSchedules(messageList, Data.CoordinatorId);
        }

        public void Handle(SendAllMessagesAtOnce message)
        {
            // TODO: make a timeout for this then just send the messsages directly to the sms actioner
            Data.CoordinatorId = message.CoordinatorId == Guid.Empty ? Data.Id : message.CoordinatorId;
            Data.OriginalScheduleStartTime = message.SendTimeUtc;
            Data.EmailAddresses = message.ConfirmationEmails;
            Data.UserOlsenTimeZone = message.UserOlsenTimeZone;
            Data.Topic = message.MetaData.Topic;
            var messageList = new List<ScheduleSmsForSendingLater>();
            for (int i = 0; i < message.Messages.Count; i++)
            {
                var smsData = new SmsData(message.Messages[i].Mobile, message.Messages[i].Message);
                var smsForSendingLater = new ScheduleSmsForSendingLater(message.SendTimeUtc, smsData, message.MetaData, Data.CoordinatorId)
                {
                    CorrelationId = Data.CoordinatorId
                };
                messageList.Add(smsForSendingLater);
            }
            Bus.Send(messageList.ToArray());
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Data.CoordinatorId,
                ScheduledMessages = messageList.Select(m => new MessageSchedule { Number = m.SmsData.Mobile, ScheduledTimeUtc = m.SendMessageAtUtc, ScheduleMessageId = m.ScheduleMessageId }).ToList(),
                CreationDateUtc = DateTime.UtcNow,
                MetaData = message.MetaData,
                ConfirmationEmailAddresses = message.ConfirmationEmails,
                UserOlsenTimeZone = message.UserOlsenTimeZone,
                MessageBody = message.Messages.First().Message,
                MessageCount = message.Messages.Count
            };
            RequestUtcTimeout<CoordinatorTimeout>(message.SendTimeUtc.AddMinutes(2));
            Bus.Publish(coordinatorCreated);
            Bus.SendLocal(new CoordinatorCreatedEmail(coordinatorCreated));
            RavenScheduleDocuments.SaveCoordinator(coordinatorCreated);
            RavenScheduleDocuments.SaveSchedules(messageList, Data.CoordinatorId);
        }

        public void Handle(PauseTrickledMessagesIndefinitely message)
        {
            if (Data.LastUpdatingCommandRequestUtc != null && Data.LastUpdatingCommandRequestUtc > message.MessageRequestTimeUtc)
                return;

            var trackingData = RavenScheduleDocuments.GetActiveScheduleTrackingData(Data.CoordinatorId);

            var messagesToPause = trackingData.Select(t => new PauseScheduledMessageIndefinitely(t.ScheduleId)).ToList();

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

            var trackingData = RavenScheduleDocuments.GetActiveScheduleTrackingData(Data.CoordinatorId);
            var resumeMessageCommands = trackingData.Select(i => new ResumeScheduledMessageWithOffset(i.ScheduleId, new TimeSpan(offset))).ToList();

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

            var trackingData = RavenScheduleDocuments.GetActiveScheduleTrackingData(Data.CoordinatorId);

            var messageResumeSpan = (rescheduleTrickledMessages.FinishTimeUtc.Ticks - rescheduleTrickledMessages.ResumeTimeUtc.Ticks);
            long messageOffset = 0;
            if (trackingData.Count > 1)
                messageOffset = messageResumeSpan / (trackingData.Count - 1);

            for (var i = 0; i < trackingData.Count; i++)
            {
                var resumeScheduledMessageWithOffset = new RescheduleScheduledMessageWithNewTime(trackingData[i].ScheduleId, new DateTime(rescheduleTrickledMessages.ResumeTimeUtc.Ticks + (i * messageOffset), DateTimeKind.Utc));
                Bus.Send(resumeScheduledMessageWithOffset);
            }

            Data.LastUpdatingCommandRequestUtc = rescheduleTrickledMessages.MessageRequestTimeUtc;
        }

        public void Timeout(CoordinatorTimeout state)
        {
            if (RavenScheduleDocuments.AreCoordinatedSchedulesComplete(Data.CoordinatorId))
            {
                Bus.Publish(new CoordinatorCompleted { CoordinatorId = Data.CoordinatorId, CompletionDateUtc = DateTime.UtcNow });
                Bus.SendLocal(CreateCompletedEmail());
                RavenScheduleDocuments.MarkCoordinatorAsComplete(Data.CoordinatorId, DateTime.UtcNow);
                MarkAsComplete();
            }
            else
            {
                var expectedMaxScheduleDate = RavenScheduleDocuments.GetMaxScheduleDateTime(Data.CoordinatorId);
                if (expectedMaxScheduleDate < DateTime.Now.AddMinutes(2))
                {
                    RequestUtcTimeout<CoordinatorTimeout>(DateTime.UtcNow.AddMinutes(2));
                }
                else
                {
                    RequestUtcTimeout<CoordinatorTimeout>(expectedMaxScheduleDate.AddMinutes(2));
                }
            }
        }

        private CoordinatorCompleteEmailWithSummary CreateCompletedEmail()
        {
            var coordinatorCompleteEmail = new CoordinatorCompleteEmailWithSummary
            {
                CoordinatorId = Data.CoordinatorId,
                EmailAddresses = Data.EmailAddresses,
                FinishTimeUtc = DateTime.UtcNow,
                StartTimeUtc = Data.OriginalScheduleStartTime,
                UserOlsenTimeZone = Data.UserOlsenTimeZone,
                Topic = Data.Topic,
            };
            var scheduleSummary = RavenScheduleDocuments.GetScheduleSummary(Data.CoordinatorId);
            var failedCounter = scheduleSummary.FirstOrDefault(s => s.Status == "Failed");
            if (failedCounter != null)
                coordinatorCompleteEmail.FailedCount = failedCounter.Count;
            var successCounter = scheduleSummary.FirstOrDefault(s => s.Status == "Sent");
            if (successCounter != null)
            {
                coordinatorCompleteEmail.SuccessCount = successCounter.Count;
                coordinatorCompleteEmail.Cost = successCounter.Cost;
            }
            return coordinatorCompleteEmail;
        }
    }

    public class CoordinatorTimeout
    {
    }

    public class CoordinateSmsSchedulingData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public DateTime OriginalScheduleStartTime { get; set; }
        
	[Unique]
        public Guid CoordinatorId { get; set; }

        public Guid CoordinatorId { get; set; }

        public DateTime? LastUpdatingCommandRequestUtc { get; set; }

        public List<string> EmailAddresses { get; set; }

        public string Topic { get; set; }

        public string UserOlsenTimeZone { get; set; }
    }
}
