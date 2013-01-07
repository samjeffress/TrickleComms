using System;
using System.Collections.Generic;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using SmsMessages.CommonData;
using SmsMessages.Scheduling.Commands;
using SmsTracking;
using SmsWeb.Models;

namespace SmsWeb.API
{
    public class Schedule
    {
        public string Number { get; set; }

        public string MessageBody { get; set; }

        public string ConfirmationEmail { get; set; }

        public DateTime ScheduledTimeUtc { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public string Topic { get; set; }

        public List<string> Tags { get; set; }
    }

    public class SmsScheduleResponse : IHasResponseStatus
    {
        public Guid RequestId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SmsScheduleService : RestServiceBase<Schedule>
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public override object OnGet(Schedule request)
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

        public override object OnPost(Schedule request)
        {
            var response = new ResponseStatus { Errors = new List<ResponseError>() };
            if (request.ScheduledTimeUtc == DateTime.MinValue)
                response.Errors.Add(new ResponseError { Message = "Start time must be set" });
            if (request.ScheduledTimeUtc < DateTime.Now.ToUniversalTime())
                response.Errors.Add(new ResponseError { Message = "Start time must not be in the past" });
            if (string.IsNullOrWhiteSpace(request.MessageBody))
                response.Errors.Add(new ResponseError { Message = "Sms message required" });
            else if (request.MessageBody.Length > 160)
                response.Errors.Add(new ResponseError { Message = "Sms message exceeds 160 character length" });
            if (request.Number == null)
                response.Errors.Add(new ResponseError { Message = "Number required" });

            if (response.Errors.Count > 0)
                return new SmsScheduleResponse {ResponseStatus = response};

            var scheduleMessage = new ScheduleSmsForSendingLater
            {
                SendMessageAtUtc = request.ScheduledTimeUtc.ToUniversalTime(),
                SmsData = new SmsData(request.Number, request.MessageBody),
                SmsMetaData = new SmsMetaData { Tags = request.Tags, Topic = request.Topic },
                ScheduleMessageId = Guid.NewGuid()
            };
            Bus.Send(scheduleMessage);
            return new SmsScheduleResponse { RequestId = scheduleMessage.ScheduleMessageId };
        }
    }
}