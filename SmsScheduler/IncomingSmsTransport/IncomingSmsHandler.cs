using NServiceBus;
using ServiceStack.ServiceClient.Web;
using SmsMessages.MessageSending.Commands;

namespace IncomingSmsTransport
{
    public class IncomingSmsHandler : IHandleMessages<MessageReceived>
    {
        public void Handle(MessageReceived message)
        {
            var serviceClient = new JsonServiceClient("http://localhost:8888/");
            serviceClient.Post<MessageReceived>("/MessageReceived/", message);
        }
    }
}
