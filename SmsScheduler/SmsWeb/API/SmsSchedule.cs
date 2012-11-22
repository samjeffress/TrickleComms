using System;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using SmsMessages.CommonData;
using SmsMessages.Scheduling;
using SmsTracking;
using SmsWeb.Models;

namespace SmsWeb.API
{
    public class SmsScheduleResponse : IHasResponseStatus
    {
        public Guid RequestId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SmsScheduleService : RestServiceBase<ScheduleModel>
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public override object OnGet(ScheduleModel request)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var smsTrackingData = session.Load<ScheduleTrackingData>(request.ScheduleMessageId.ToString());
                if (smsTrackingData == null)
                    return new SmsScheduleResponse { RequestId = request.ScheduleMessageId, ResponseStatus = new ResponseStatus("NotFound") };
                return new ScheduleModel
                {
                    ScheduleMessageId = request.ScheduleMessageId,
                    // TODO : Fill this out
                    //Status = smsTrackingData.MessageStatus.ToString()
                };
            }
        }

        public override object OnPost(ScheduleModel request)
        {
            if (IsValidRequest(request))
            {
                var scheduleMessage = new ScheduleSmsForSendingLater
                {
                    SendMessageAt = request.ScheduledTime,
                    SmsData = new SmsData(request.Number, request.MessageBody),
                    SmsMetaData = new SmsMetaData { Tags = request.Tags, Topic = request.Topic },
                    ScheduleMessageId = Guid.NewGuid()
                };
                Bus.Send(scheduleMessage);
                return new SmsScheduleResponse { RequestId = scheduleMessage.ScheduleMessageId };
            }
            return new SmsScheduleResponse { ResponseStatus = new ResponseStatus("InvalidRequest") };
        }

        private bool IsValidRequest(ScheduleModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Number) || string.IsNullOrWhiteSpace(request.MessageBody) || request.ScheduledTime <= DateTime.Now)
            {
                return false;
            }
            return true;
        }
    }
}