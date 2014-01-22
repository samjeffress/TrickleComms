using System;
using NServiceBus;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;
using SmsMessages.MessageSending.Responses;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;
using SmsTrackingModels;

namespace SmsScheduler
{
    public class ScheduleSms : 
        Saga<ScheduledSmsData>,
        IAmStartedByMessages<ScheduleSmsForSendingLater>,
        IHandleTimeouts<ScheduleSmsTimeout>,
        IHandleMessages<PauseScheduledMessageIndefinitely>,
        IHandleMessages<ResumeScheduledMessageWithOffset>,
        IHandleMessages<RescheduleScheduledMessageWithNewTime>,
        IHandleMessages<MessageSent>,
        IHandleMessages<MessageFailedSending>
    {
        public IRavenDocStore RavenDocStore { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<MessageSent>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<MessageFailedSending>(data => data.Id, message => message.CorrelationId);
            ConfigureMapping<PauseScheduledMessageIndefinitely>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            ConfigureMapping<ResumeScheduledMessageWithOffset>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            ConfigureMapping<RescheduleScheduledMessageWithNewTime>(data => data.ScheduleMessageId, message => message.ScheduleMessageId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(ScheduleSmsForSendingLater scheduleSmsForSendingLater)
        {
            Data.OriginalMessage = scheduleSmsForSendingLater;
            Data.ScheduleMessageId = scheduleSmsForSendingLater.ScheduleMessageId == Guid.NewGuid() ? Data.Id : scheduleSmsForSendingLater.ScheduleMessageId;
            Data.RequestingCoordinatorId = scheduleSmsForSendingLater.CorrelationId;
            Data.TimeoutCounter = 0;
            var timeout = new DateTime(scheduleSmsForSendingLater.SendMessageAtUtc.Ticks, DateTimeKind.Utc);
            RequestUtcTimeout(timeout, new ScheduleSmsTimeout { TimeoutCounter = 0});
            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(scheduleSmsForSendingLater.ScheduleMessageId.ToString());
                if (scheduleTrackingData == null)
                {
                    var scheduleTracker = new ScheduleTrackingData
                    {
                        MessageStatus = MessageStatus.Scheduled,
                        ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId,
                        SmsData = scheduleSmsForSendingLater.SmsData,
                        SmsMetaData = scheduleSmsForSendingLater.SmsMetaData,
                        ScheduleTimeUtc = scheduleSmsForSendingLater.SendMessageAtUtc
                    };
                    session.Store(scheduleTracker, scheduleSmsForSendingLater.ScheduleMessageId.ToString());
                }
                else
                {
                    scheduleTrackingData.MessageStatus = MessageStatus.Scheduled;    
                }
                
                session.SaveChanges();
            }
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

        public void Handle(MessageSent message)
        {
            Bus.Publish(new ScheduledSmsSent { CoordinatorId = Data.RequestingCoordinatorId, ScheduledSmsId = Data.ScheduleMessageId, ConfirmationData = message.ConfirmationData, Number = message.SmsData.Mobile});
            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(Data.ScheduleMessageId.ToString());
                scheduleTrackingData.ConfirmationData = message.ConfirmationData;
                scheduleTrackingData.MessageStatus = MessageStatus.Sent;
                session.SaveChanges();
            }
            MarkAsComplete();
        }

        public void Handle(PauseScheduledMessageIndefinitely pauseScheduling)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > pauseScheduling.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = true;
            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(Data.ScheduleMessageId.ToString());
                scheduleTrackingData.MessageStatus = MessageStatus.Paused;
                session.SaveChanges();
            }
            Bus.Publish(new MessageSchedulePaused { CoordinatorId = Data.RequestingCoordinatorId, ScheduleId = pauseScheduling.ScheduleMessageId, Number = Data.OriginalMessage.SmsData.Mobile });
            Data.LastUpdateCommandRequestUtc = pauseScheduling.MessageRequestTimeUtc;
        }

        public void Handle(ResumeScheduledMessageWithOffset scheduleSmsForSendingLater)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > scheduleSmsForSendingLater.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = false;
            var rescheduledTime = Data.OriginalMessage.SendMessageAtUtc.Add(scheduleSmsForSendingLater.Offset);
            Data.TimeoutCounter++;
            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(Data.ScheduleMessageId.ToString());
                scheduleTrackingData.MessageStatus = MessageStatus.Scheduled;
                scheduleTrackingData.ScheduleTimeUtc = rescheduledTime;
                session.SaveChanges();
            }
            RequestUtcTimeout(rescheduledTime, new ScheduleSmsTimeout { TimeoutCounter = Data.TimeoutCounter });
            Bus.Publish(new MessageRescheduled { CoordinatorId = Data.RequestingCoordinatorId, ScheduleMessageId = Data.ScheduleMessageId, RescheduledTimeUtc = rescheduledTime, Number = Data.OriginalMessage.SmsData.Mobile });
            Data.LastUpdateCommandRequestUtc = scheduleSmsForSendingLater.MessageRequestTimeUtc;
        }

        public void Handle(RescheduleScheduledMessageWithNewTime message)
        {
            if (Data.LastUpdateCommandRequestUtc != null && Data.LastUpdateCommandRequestUtc > message.MessageRequestTimeUtc)
                return;
            Data.SchedulingPaused = false;
            Data.TimeoutCounter++;
            RequestUtcTimeout(message.NewScheduleTimeUtc, new ScheduleSmsTimeout { TimeoutCounter = Data.TimeoutCounter });
            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(Data.ScheduleMessageId.ToString());
                scheduleTrackingData.MessageStatus = MessageStatus.Scheduled;
                scheduleTrackingData.ScheduleTimeUtc = message.NewScheduleTimeUtc;
                session.SaveChanges();
            }
            Bus.Publish(new MessageRescheduled { CoordinatorId = Data.RequestingCoordinatorId, ScheduleMessageId = Data.ScheduleMessageId, RescheduledTimeUtc = message.NewScheduleTimeUtc, Number = Data.OriginalMessage.SmsData.Mobile });
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

            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(Data.ScheduleMessageId.ToString());
                scheduleTrackingData.MessageStatus = MessageStatus.Failed;
                scheduleTrackingData.SmsFailureData = failedMessage.SmsFailed;
                session.SaveChanges();
            }
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
    }

    public class ScheduleSmsTimeout
    {
        public int TimeoutCounter { get; set; }
    }
}