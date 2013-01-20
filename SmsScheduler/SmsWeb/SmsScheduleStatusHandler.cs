using Microsoft.AspNet.SignalR;
using NServiceBus;
using SmsMessages.Scheduling.Events;

namespace SmsWeb
{
    public class SmsScheduleStatusHandler : IHandleMessages<ScheduledSmsSent>
        , IHandleMessages<ScheduledSmsFailed>
        //, IHandleMessages<MessageSchedulePaused>
    {
        // TODO: SmsScheduleStatusHandler : Move formatting to client
        public void Handle(ScheduledSmsSent message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            var html = string.Format(
                "Number: {0} <br /> " +
                "Status:CompletedSuccess <br/> " +
                "Time Sent: {1} <br />" + 
                "Cost: {2}", message.Number, message.ConfirmationData.SentAtUtc.ToLocalTime(), message.ConfirmationData.Price);
            context.Clients.All.updateSchedule(new {ScheduleId = message.ScheduledSmsId, Body = html, Class = "success"});
        }


        public void Handle(ScheduledSmsFailed message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
            var html = string.Format(
                "Number: {0} <br /> " +
                "Status: CompletedFailure <br/> " +
                "Sending Failed: {1}", message.Number, message.SmsFailedData.Message );
            context.Clients.All.updateSchedule(new { ScheduleId = message.ScheduledSmsId, Body = html, Class = "fail" });
        }

        //public void Handle(MessageSchedulePaused message)
        //{
        //    var context = GlobalHost.ConnectionManager.GetHubContext<ScheduleStatus>();
        //    var html = string.Format(
        //        "Number: {0} <br /> " +
        //        "Status:CompletedSuccess <br/> " +
        //        "Time Sent: {1} <br />" +
        //        "Cost: {2}", message..Number, message.ConfirmationData.SentAtUtc.ToLocalTime(), message.ConfirmationData.Price);
        //    context.Clients.All.updateScheduleClassOnly(new { ScheduleId = message.ScheduledSmsId, Body = html, Class = "success" });
        //}
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