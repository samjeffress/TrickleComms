using System;
using System.Collections.Generic;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsTrackingModels;

namespace SmsWeb.API
{
    public class Sms
    {
        public Guid RequestId { get; set; }

        public string Number { get; set; }

        public string Message { get; set; }

        public string ConfirmationEmailAddress { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }

        public string Status { get; set; }
    }

    public class SmsResponse : IHasResponseStatus
    {
        public Guid RequestId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SmsService : RestServiceBase<Sms>
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public override object OnPost(Sms request)
        {
            var responseStatus = new ResponseStatus {Errors = new List<ResponseError>()};
            if (string.IsNullOrWhiteSpace(request.Number))
                responseStatus.Errors.Add(new ResponseError { FieldName = "Number", Message = "Sms number must be set" });
            if (string.IsNullOrWhiteSpace(request.Message))
                responseStatus.Errors.Add(new ResponseError { FieldName = "Message", Message = "Sms message must be set" });
            else if (request.Message.Length > 160)
                responseStatus.Errors.Add(new ResponseError { FieldName = "Message", Message = "Sms message must not exceed 160 characters"});
            if (responseStatus.Errors.Count > 0)
                return new SmsResponse {ResponseStatus = responseStatus};

            if (request.RequestId == Guid.Empty)
                request.RequestId = Guid.NewGuid();

            Bus.Send(new SendOneMessageNow {CorrelationId = request.RequestId, SmsData = new SmsData(request.Number, request.Message), ConfirmationEmailAddress = request.ConfirmationEmailAddress, SmsMetaData = new SmsMetaData { Tags = request.Tags, Topic = request.Topic }});
            return new SmsResponse {RequestId = request.RequestId};
        }

        public override object OnGet(Sms request)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var smsTrackingData = session.Load<SmsTrackingData>(request.RequestId.ToString());
                if (smsTrackingData == null)
                    return new SmsResponse { RequestId = request.RequestId, ResponseStatus = new ResponseStatus("NotYetComplete", "Sms has not yet been completed.") };
                return new Sms
                {
                    RequestId = request.RequestId,
                    Status = smsTrackingData.Status.ToString()
                };
            }
        }
    }
}