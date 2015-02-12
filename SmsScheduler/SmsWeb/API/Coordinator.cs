using System;
using System.Collections.Generic;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using SmsMessages.Coordinator.Commands;
using SmsTrackingModels;

namespace SmsWeb.API
{
    public class Coordinator
    {
        public Guid RequestId { get; set; }

        public List<string> Numbers { get; set; }

        public string Message { get; set; }

        public DateTime StartTimeUtc { get; set; }

		[Obsolete("this refers to 'coordinator separated by defined time' which is no longer available")]
        public TimeSpan? TimeSeparator { get; set; }

        public DateTime? SendAllByUtc { get; set; }

        public bool SendAllAtOnce { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }

        public List<string> ConfirmationEmails { get; set; }

        public string OlsenTimeZone { get; set; }
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
                var response = new CoordinatorResponse { RequestId = request.RequestId };
                if (trackingData == null)
                {
                    response.ResponseStatus = new ResponseStatus("NotFound");
                    return response;
                }

                response.Messages = trackingData.GetListOfCoordinatedSchedules(RavenDocStore.GetStore());
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
            if (!MessageTypeValid(request))
                response.Errors.Add(new ResponseError { Message = "Message must contain either Time Separator OR DateTime to send all messages by." });
            if (string.IsNullOrWhiteSpace(request.Topic))
                response.Errors.Add(new ResponseError { Message = "Topic must be set" });

            var coordinatorResponse = new CoordinatorResponse {ResponseStatus = response};
            if (response.Errors.Count > 0)
                response.ErrorCode = "InvalidMessage";

            if (response.Errors.Count == 0)
            {
                if (request.RequestId == Guid.Empty)
                    coordinatorResponse.RequestId = Guid.NewGuid();
                else
                    coordinatorResponse.RequestId = request.RequestId;
				if (GetMessageTypeFromModel (request) == typeof(TrickleSmsOverCalculatedIntervalsBetweenSetDates)) {
					var message = Mapper.MapToTrickleOverPeriod (request, coordinatorResponse.RequestId);
					Bus.Send (message);
				} else if (GetMessageTypeFromModel (request) == typeof(SendAllMessagesAtOnce)) {
					var message = Mapper.MapToSendAllAtOnce (request, coordinatorResponse.RequestId);
					Bus.Send (message);
				} else {
					throw new NotImplementedException ("This option has been removed");
				}
            }

            return coordinatorResponse;
        }

        private bool MessageTypeValid(Coordinator request)
        {
            try
            {
                GetMessageTypeFromModel(request);
                return true;
            }
            catch
            {
                return false;
            }
             
        }

        private Type GetMessageTypeFromModel(Coordinator request)
        {
            Type requestType = typeof (object);
            var trueCount = 0;
            if (request.SendAllByUtc.HasValue)
            {
                requestType = typeof (TrickleSmsOverCalculatedIntervalsBetweenSetDates);
                trueCount++;
            }
            if (request.SendAllAtOnce)
            {
                requestType = typeof (SendAllMessagesAtOnce);
                trueCount++;
            }
            if (trueCount != 1)
                throw new ArgumentException("Cannot determine which message type to send");
            return requestType;
        }
    }
}