using NServiceBus;
using ServiceStack.ServiceHost;
using SmsMessages.MessageSending.Commands;

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