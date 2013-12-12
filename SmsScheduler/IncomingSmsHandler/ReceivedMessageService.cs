using NServiceBus;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using SmsMessages.MessageSending.Commands;

namespace IncomingSmsHandler
{
    public class ReceivedMessageService : IService
    {
        public IBus Bus { get; set; }

        //[Authenticate]
        // TODO: Send Empty Twiml Response - http://twimlets.com/echo?Twiml=%3CResponse%3E%3C%2FResponse%3E
        public void Any(MessageReceived request)
        {
            Bus.Send("IncomingSmsHandler", request);
        }
    }
}