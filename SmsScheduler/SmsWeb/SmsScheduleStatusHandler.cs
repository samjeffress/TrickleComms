using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Events;
using SmsMessages.Scheduling.Events;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;

namespace SmsWeb
{
    public class SmsScheduleStatusHandler : IHandleMessages<ScheduledSmsSent>
        , IHandleMessages<ScheduledSmsFailed>
        , IHandleMessages<MessageSchedulePaused>
        , IHandleMessages<MessageRescheduled>
        , IHandleMessages<SmsScheduled>
        , IHandleMessages<CoordinatorCompleted>
        , IHandleMessages<CoordinatorCreated>
    {
        //public SmsScheduleStatusHandler()
        //{
            
        //}

        //public SmsScheduleStatusHandler(IRavenDocStore ravenDocStore)
        //{
        //    RavenDocStore = ravenDocStore;
        //}
        public IRavenDocStore RavenDocStore { get; set; }

        public void Handle(ScheduledSmsSent message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            UpdateCoordinatorData(message.CoordinatorId, context);
        }

        public void Handle(ScheduledSmsFailed message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            UpdateCoordinatorData(message.CoordinatorId, context);
        }

        public void Handle(SmsScheduled message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            UpdateCoordinatorData(message.CoordinatorId, context);
        }

        public void Handle(MessageRescheduled message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            UpdateCoordinatorData(message.CoordinatorId, context);
        }

        public void Handle(MessageSchedulePaused message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            UpdateCoordinatorData(message.CoordinatorId, context);
        }

        public void Handle(CoordinatorCompleted message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.coordinatorCompleted(new
            {
                CoordinatorId = message.CoordinatorId,
                CompletedAt = message.CompletionDateUtc,
                Class = "completed"
            });
        }

        public void Handle(CoordinatorCreated message)
        {
            Thread.Sleep(1000);
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.coordinatorStarted(new
            {
                message.CoordinatorId
            });
        }
        
        private void UpdateCoordinatorData(Guid coordinatorId, IHubContext context)
        {
            if (coordinatorId != Guid.Empty)
            {
                using (var session = RavenDocStore.GetStore().OpenSession())
                {
                    var coordinatorSummary = session.Query<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult, ScheduledMessagesStatusCountInCoordinatorIndex>()
                            .Where(s => s.CoordinatorId == coordinatorId.ToString())
                            .ToList();

                    var sentSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Sent.ToString());
                    var failedSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Failed.ToString());
                    var scheduledSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Scheduled.ToString());
                    var cancelledSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Cancelled.ToString());
                    var waitingForSchedulingSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.WaitingForScheduling.ToString());
                    var pausedSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Paused.ToString());

                    context.Clients.All.messageStatusUpdated(
                        coordinatorId = coordinatorId,
                        sentSummary != null ? sentSummary.Count : 0,
                        failedSummary != null ? failedSummary.Count : 0,
                        scheduledSummary != null ? scheduledSummary.Count : 0,
                        pausedSummary != null ? pausedSummary.Count : 0,
                        waitingForSchedulingSummary != null ? waitingForSchedulingSummary.Count : 0,
                        cancelledSummary != null ? cancelledSummary.Count : 0
                    );
                }
            }
        }
    }

    public class ScheduleStatus : Hub
    {
        public void Send(ScheduledSmsSent message)
        {
            Clients.All.updateSchedule(
                new
                    {
                        ScheduleId = message.ScheduledSmsId,
                        Status = message.ConfirmationData.Price,
                        TimeSent = message.ConfirmationData.SentAtUtc.ToLongDateString()
                    });
        }
    }
}