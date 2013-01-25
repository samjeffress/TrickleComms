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
    {
        public void Handle(ScheduledSmsSent message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.scheduleSent(new
                {
                    ScheduleId = message.ScheduledSmsId, 
                    Number = message.Number, 
                    SentAt = message.ConfirmationData.SentAtUtc.ToLocalTime(), 
                    Cost = message.ConfirmationData.Price, 
                    Class = "success"
                });
        }

        public void Handle(ScheduledSmsFailed message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.updateSchedule(new
                {
                    ScheduleId = message.ScheduledSmsId, 
                    Number = message.Number,
                    SendFailedMessage = message.SmsFailedData.Message,
                    Class = "fail"
                });
        }

        public void Handle(SmsScheduled message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.messageScheduled(new
                {
                    ScheduleId = message.ScheduleMessageId, 
                    Number = message.SmsData.Mobile,
                    ScheduledTime = message.ScheduleSendingTimeUtc.ToLocalTime(),
                    Class = "waiting"
                });
        }

        public void Handle(MessageRescheduled message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.messageScheduled(new
                {
                    ScheduleId = message.ScheduleMessageId, 
                    Number = message.Number,
                    ScheduledTime = message.RescheduledTimeUtc.ToLocalTime(),
                    Class = "waiting"
                });
        }

        public void Handle(MessageSchedulePaused message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            context.Clients.All.messagePaused(new
                {
                    ScheduleId = message.ScheduleId, 
                    Number = message.Number,
                    Class = "paused"
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