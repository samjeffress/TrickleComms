using System;
using System.Collections.Generic;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using SmsTracking;

namespace SmsWeb.API
{
    public class Coordinator
    {
        public Guid RequestId { get; set; }

        public List<string> Numbers { get; set; }

        public string Message { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public TimeSpan? TimeSeparator { get; set; }

        public DateTime? SendAllByUtc { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }
    }

    public class CoordinatorResponse : IHasResponseStatus
    {
        public Guid RequestId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }

        public List<MessageSendingStatus> Messages { get; set; }

        public string CoordinatorStatus { get; set; }
    }

    public class CoordinatorService : RestServiceBase<Coordinator>
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public ICoordinatorApiModelToMessageMapping Mapper { get; set; }

        public override object OnGet(Coordinator request)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(request.RequestId.ToString());
                var response = new CoordinatorResponse  { RequestId = request.RequestId };
                if (trackingData == null)
                {
                    response.ResponseStatus = new ResponseStatus("NotFound");
                    return response;
                }
                
                response.Messages = trackingData.MessageStatuses;
                response.CoordinatorStatus = trackingData.CurrentStatus.ToString();
                return response;
            }
        }

        public override object OnPost(Coordinator request)
        {
            var response = new ResponseStatus { Errors = new List<ResponseError>() };
            if (request.StartTimeUtc == DateTime.MinValue)
                response.Errors.Add(new ResponseError { Message = "Start time must be set" });
            if (request.StartTimeUtc < DateTime.Now.ToUniversalTime())
                response.Errors.Add(new ResponseError { Message = "Start time must not be in the past" });
            if (string.IsNullOrWhiteSpace(request.Message))
                response.Errors.Add(new ResponseError {Message = "Sms Message Required"});
            else if (request.Message.Length > 160)
                response.Errors.Add(new ResponseError { Message = "Sms exceeds 160 character length"});
            if (request.Numbers == null || request.Numbers.Count == 0)
                response.Errors.Add(new ResponseError {Message = "List of numbers required"});
            if ((request.SendAllByUtc.HasValue && request.TimeSeparator.HasValue) || (!request.SendAllByUtc.HasValue && !request.TimeSeparator.HasValue))
                response.Errors.Add(new ResponseError { Message = "Message must contain either Time Separator OR DateTime to send all messages by." });

            var coordinatorResponse = new CoordinatorResponse {ResponseStatus = response};
            if (response.Errors.Count > 0)
                response.ErrorCode = "InvalidMessage";

            if (response.Errors.Count == 0)
            {
                if (request.RequestId == Guid.Empty)
                    coordinatorResponse.RequestId = Guid.NewGuid();
                else
                    coordinatorResponse.RequestId = request.RequestId;
                if (request.TimeSeparator.HasValue && !request.SendAllByUtc.HasValue)
                {
                    var message = Mapper.MapToTrickleSpacedByPeriod(request, coordinatorResponse.RequestId);
                    Bus.Send(message);
                }
                if (!request.TimeSeparator.HasValue && request.SendAllByUtc.HasValue)
                {
                    var message = Mapper.MapToTrickleOverPeriod(request, coordinatorResponse.RequestId);
                    Bus.Send(message);
                }
            }

            return coordinatorResponse;
        }
    }
}