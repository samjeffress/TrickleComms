using NServiceBus;
using SmsMessages.MessageSending.Responses;
using SmsTrackingModels;

namespace SmsActioner
{
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