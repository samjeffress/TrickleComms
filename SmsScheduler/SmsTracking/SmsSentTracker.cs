using NServiceBus;
using SmsMessages.MessageSending.Events;
using SmsTrackingModels;

namespace SmsTracking
{
    public class SmsSentTracker : 
        IHandleMessages<MessageSent>, IHandleMessages<MessageFailedSending>
    {
        public IRavenDocStore RavenStore { get; set; }

        //public IEmailService EmailService { get; set; }

        // TODO: Check why RavenStore is always being set as null through container
        public void Handle(MessageSent message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var messageSent = session.Load<SmsTrackingData>(message.CorrelationId);
                if (messageSent != null) return;
                session.Store(new SmsTrackingData(message), message.CorrelationId.ToString());
                session.SaveChanges();
            }

            //if(!string.IsNullOrWhiteSpace(message.ConfirmationEmailAddress))
            //{
            //    EmailService.SendSmsSentConfirmation(message);
            //}
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

            //if (!string.IsNullOrWhiteSpace(message.ConfirmationEmailAddress))
            //{
            //    EmailService.SendSmsFailedConfirmation(message);
            //}
        }
    }

}
