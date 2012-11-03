using NServiceBus;
using SmsMessages.MessageSending;

namespace SmsTracking
{
    public class SmsSentTracker : 
        IHandleMessages<MessageSent>
    {
        public IRavenDocStore RavenStore { get; set; }

        public IEmailService EmailService { get; set; }

        public void Handle(MessageSent message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var messageSent = session.Load<MessageSent>(message.ConfirmationData.Receipt);
                if (messageSent != null) return;
                session.Store(message, message.ConfirmationData.Receipt);
                session.SaveChanges();
            }

            if(!string.IsNullOrWhiteSpace(message.ConfirmationEmailAddress))
            {
                EmailService.SendSmsSentConfirmation(message);
            }
        }
    }
}
