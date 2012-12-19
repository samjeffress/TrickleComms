using NServiceBus;
using SmsMessages.MessageSending.Events;

namespace SmsTracking
{
    public class SmsSentTracker : 
        IHandleMessages<MessageSent>, IHandleMessages<MessageFailedSending>
    {
        public IRavenDocStore RavenStore { get; set; }

        public IEmailService EmailService { get; set; }

        public void Handle(MessageSent message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var messageSent = session.Load<SmsTrackingData>(message.CorrelationId);
                if (messageSent != null) return;
                session.Store(new SmsTrackingData(message), message.CorrelationId.ToString());
                session.SaveChanges();
            }

            if(!string.IsNullOrWhiteSpace(message.ConfirmationEmailAddress))
            {
                EmailService.SendSmsSentConfirmation(message);
            }
        }

        public void Handle(MessageFailedSending message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var messageSent = session.Load<SmsTrackingData>(message.CorrelationId);
                if (messageSent != null) return;
                session.Store(new SmsTrackingData(message), message.CorrelationId.ToString());
                session.SaveChanges();
            }

            if (!string.IsNullOrWhiteSpace(message.ConfirmationEmailAddress))
            {
                EmailService.SendSmsFailedConfirmation(message);
            }
        }
    }

    public enum MessageTrackedStatus
    {
        Sent, Failed
    }
}
