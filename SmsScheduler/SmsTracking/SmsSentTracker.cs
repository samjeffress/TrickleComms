using NServiceBus;
using Raven.Client;
using SmsMessages.MessageSending;

namespace SmsTracking
{
    public class SmsSentTracker : 
        IHandleMessages<MessageSent>
    {
        public IDocumentStore DocumentStore { get; set; }

        public void Handle(MessageSent message)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var messageSent = session.Load<MessageSent>(message.ConfirmationData.Receipt);
                if (messageSent != null) return;
                session.Store(message, message.ConfirmationData.Receipt);
                session.SaveChanges();
            }
        }
    }
}
