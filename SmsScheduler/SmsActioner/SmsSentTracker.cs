using NServiceBus;
using SmsMessages.MessageSending.Responses;
using SmsTrackingModels;

namespace SmsActioner
{
    public class SmsSentTracker :
        IHandleMessages<MessageSuccessfullyDelivered>, 
        IHandleMessages<MessageFailedSending>
    {
        public IRavenDocStore RavenStore { get; set; }

        public void Handle(MessageSuccessfullyDelivered message)
        {
            using (var session = RavenStore.GetStore().OpenSession(RavenStore.DatabaseName()))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var messageSent = session.Load<SmsTrackingData>(message.CorrelationId);
                if (messageSent != null) return;
                session.Store(new SmsTrackingData(message), message.CorrelationId.ToString());
                session.SaveChanges();
            }
        }

        public void Handle(MessageFailedSending message)
        {
            using (var session = RavenStore.GetStore().OpenSession(RavenStore.DatabaseName()))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var messageSent = session.Load<SmsTrackingData>(message.CorrelationId);
                if (messageSent != null) return;
                session.Store(new SmsTrackingData(message), message.CorrelationId.ToString());
                session.SaveChanges();
            }
        }
    }

    public class EmailSentTracker :
        IHandleMessages<EmailStatusUpdate>
    {
        public IRavenDocStore RavenStore { get; set; }

        public void Handle(EmailStatusUpdate message)
        {
            using (var session = RavenStore.GetStore().OpenSession(RavenStore.DatabaseName()))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var emailTrackingData = session.Load<EmailTrackingData>(message.CorrelationId);
                if (emailTrackingData != null)
                    emailTrackingData.EmailStatus = message.Status;
                else
                {
                    session.Store(new EmailTrackingData(message) { EmailStatus = message.Status }, message.CorrelationId.ToString());
                }
                session.SaveChanges();
            }
        }
    }
}