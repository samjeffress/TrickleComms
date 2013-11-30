using Funq;
using ServiceStack.WebHost.Endpoints;
using SmsMessages.MessageSending.Events;

namespace IncomingSmsHandler
{
    public class AppHost : AppHostHttpListenerBase
    {
        public override void Configure(Container container)
        {
            Routes
                .Add<MessageReceived>("/SmsIncoming");

            EndpointConfig.ConfigureNServiceBus();
            Container.Register(EndpointConfig.Bus);
        }
    }
}