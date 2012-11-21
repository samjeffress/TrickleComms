using System;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using SmsMessages.MessageSending;

namespace SmsWeb.API
{
    public class Sms
    {
        public Guid RequestId { get; set; }

        public string Number { get; set; }

        public string Message { get; set; }
    }

    public class SmsResponse : IHasResponseStatus
    {
        public Guid RequestId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SmsService : RestServiceBase<Sms>
    {
        public IBus Bus { get; set; }

        public override object OnGet(Sms request)
        {
            var requestId = Guid.NewGuid();
            Bus.Send(new SendOneMessageNow {CorrelationId = requestId});
            return new SmsResponse {RequestId = requestId};
        }
    }
}