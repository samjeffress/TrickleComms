using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;

namespace SmsWeb.API
{
    public class Coordinator
    {
        public List<string> Numbers { get; set; }

        public string Message { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan? TimeSeparator { get; set; }

        public DateTime? SendAllBy { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }
    }

    public class CoordinatorResponse : IHasResponseStatus
    {
        public Guid RequestId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CoordinatorService : RestServiceBase<Coordinator>
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public override object OnGet(Coordinator request)
        {
            return base.OnGet(request);
        }

        public override object OnPost(Coordinator request)
        {
            var response = new ResponseStatus { Errors = new List<ResponseError>() };
            if (request.StartTime == DateTime.MinValue)
                response.Errors.Add(new ResponseError { Message = "Start time must be set" });
            if (request.StartTime < DateTime.Now)
                response.Errors.Add(new ResponseError { Message = "Start time must not be in the past" });
            if (string.IsNullOrWhiteSpace(request.Message))
                response.Errors.Add(new ResponseError {Message = "Sms Message Required"});
            if (request.Numbers == null || request.Numbers.Count == 0)
                response.Errors.Add(new ResponseError {Message = "List of numbers required"});
            if ((request.SendAllBy.HasValue && request.TimeSeparator.HasValue) || (!request.SendAllBy.HasValue && !request.TimeSeparator.HasValue))
                response.Errors.Add(new ResponseError { Message = "Message must contain either Time Separator OR DateTime to send all messages by." });

            var coordinatorResponse = new CoordinatorResponse {ResponseStatus = response};
            if (response.Errors.Count > 0)
                response.ErrorCode = "InvalidMessage";

            if (response.Errors.Count == 0)
            {
                coordinatorResponse.RequestId = Guid.NewGuid();
                if (request.TimeSeparator.HasValue && !request.SendAllBy.HasValue)
                {
                    var trickleSmsOverTimePeriod = new TrickleSmsWithDefinedTimeBetweenEachMessage
                    {
                        Messages = request.Numbers.Select(n => new SmsData(n, request.Message)).ToList(),
                        StartTime = request.StartTime,
                        TimeSpacing = request.TimeSeparator.Value,
                        MetaData = new SmsMetaData { Tags = request.Tags, Topic = request.Topic }
                    };
                    Bus.Send(trickleSmsOverTimePeriod);
                }
                if (!request.TimeSeparator.HasValue && request.SendAllBy.HasValue)
                {
                    var trickleSmsSpacedByTimePeriod = new TrickleSmsOverCalculatedIntervalsBetweenSetDates
                    {
                        Duration = request.SendAllBy.Value.Subtract(request.StartTime),
                        Messages = request.Numbers.Select(n => new SmsData(n, request.Message)).ToList(),
                        StartTime = request.StartTime,
                        MetaData = new SmsMetaData { Tags = request.Tags, Topic = request.Topic },
                        CoordinatorId = coordinatorResponse.RequestId
                    };
                    Bus.Send(trickleSmsSpacedByTimePeriod);
                }
            }

            return coordinatorResponse;
        }
    }
}