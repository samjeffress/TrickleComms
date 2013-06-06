using System.Threading;
using Microsoft.AspNet.SignalR;
using NServiceBus;
using SmsMessages.Coordinator.Events;
using SmsMessages.Scheduling.Events;

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
        public IRavenDocStore RavenDocStore { get; set; }

        public void Handle(ScheduledSmsSent message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.scheduleUpdated(new
            {
                message.CoordinatorId
            });
        }

        public void Handle(ScheduledSmsFailed message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.scheduleUpdated(new
            {
                message.CoordinatorId
            });
        }

        public void Handle(SmsScheduled message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.scheduleUpdated(new
                {
                    message.CoordinatorId
                });
        }

        public void Handle(MessageRescheduled message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.scheduleUpdated(new
            {
                message.CoordinatorId
            });
        }

        public void Handle(MessageSchedulePaused message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.scheduleUpdated(new
            {
                message.CoordinatorId
            });
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