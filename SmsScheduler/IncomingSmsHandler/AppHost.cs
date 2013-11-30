using Funq;
using ServiceStack.WebHost.Endpoints;
using SmsMessages.MessageSending.Events;

namespace IncomingSmsHandler
{
    public class AppHost : AppHostHttpListenerBase
    {
        public override void Configure(Container container)
        {
            // TODO: wire up bus for IOC
            Routes
                .Add<MessageReceived>("/SmsIncoming");
        }
    }
}