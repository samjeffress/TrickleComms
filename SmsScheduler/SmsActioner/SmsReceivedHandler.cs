using System;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace SmsActioner
{
    public class SmsReceivedHandler : Service
    {
        public void Get(SmsReceieved smsReceieved)
        {
            Console.WriteLine("Wassup!");
        }
    }

    public class SmsReceieved
    {
        public string MessageId { get; set; }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("StarterTemplate HttpListener", typeof(SmsReceieved).Assembly) { }

        public override void Configure(Funq.Container container)
        {
            Routes.Add<SmsReceieved>("/SmsReceived/{messageId}");
        }
    }
}