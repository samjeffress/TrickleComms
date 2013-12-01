using NServiceBus;
using ServiceStack.ServiceHost;
using SmsMessages.MessageSending.Events;

namespace IncomingSmsHandler
{
    public class ReceivedMessageService : IService
    {
        public IBus Bus { get; set; }

        public void Any(MessageReceived request)
        {
            Bus.Send("IncomingSmsHandler", request);
        }
    }
}