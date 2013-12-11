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
        public void Any(MessageReceived request)
        {
            Bus.Send("IncomingSmsHandler", request);
        }
    }
}