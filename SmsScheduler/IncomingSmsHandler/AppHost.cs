using Funq;
using NServiceBus;
using ServiceStack.WebHost.Endpoints;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(IncomingSmsHandler.AppHost), "Start")]
namespace IncomingSmsHandler
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("Incoming Sms", typeof(ReceivedMessageService).Assembly)
        {
        }

        public static void Start()
        {
            new AppHost().Init();
        }

        public override void Configure(Container container)
        {
            Routes
                .Add<MessageReceived>("/SmsIncoming");

            EndpointConfig.ConfigureNServiceBus();
            Container.Register<IBus>(b => EndpointConfig.Bus);
        }
    }
}