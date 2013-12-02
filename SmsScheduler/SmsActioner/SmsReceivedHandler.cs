using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;
using SmsMessages.MessageSending.Commands;

namespace SmsActioner
{
    public class SmsMessageReceivedHandler : Service
    {
        public IBus Bus { get; set; }

        public void Post(MessageReceived smsReceieved)
        {
            Bus.SendLocal(smsReceieved);
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("StarterTemplate HttpListener", typeof(SmsMessageReceivedHandler).Assembly) { }

        public override void Configure(Funq.Container container)
        {
            Routes
                .Add<MessageReceived>("/MessageReceived/");
        }
    }
}