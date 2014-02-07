using System;
using NServiceBus;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsTrackingModels;

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

    public class MessageReceivedHandler : IHandleMessages<MessageReceived>
    {
        public IRavenDocStore RavenStore { get; set; }

        public void Handle(MessageReceived message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                session.Store(new SmsReceivedData { SmsId = Guid.Parse(message.Sid), SmsConfirmationData = new SmsConfirmationData(null, message.DateSent, message.Price), SmsData = new SmsData(message.From, message.Body)}, message.Sid);
                session.SaveChanges();
            }
        }
    }
}