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
        IHandleMessages<EmailDelivered>,
        IHandleMessages<EmailDeliveredAndOpened>,
        IHandleMessages<EmailDeliveredAndClicked>,
        IHandleMessages<EmailDeliveryFailed>,
        IHandleMessages<EmailUnsubscribed>,
        IHandleMessages<EmailComplained>
    {
        public IRavenDocStore RavenStore { get; set; }

        public void Handle(EmailDelivered message)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(EmailDeliveredAndOpened message)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(EmailDeliveredAndClicked message)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(EmailUnsubscribed message)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(EmailComplained message)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(EmailDeliveryFailed message)
        {
            using (var session = RavenStore.GetStore().OpenSession(RavenStore.DatabaseName()))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var emailTrackingData = session.Load<EmailTrackingData>(message.CorrelationId);
                if (emailTrackingData != null)
                    emailTrackingData.EmailStatus = "DeliveryFailed";
                else
                {
                    session.Store(new EmailTrackingData(message), message.CorrelationId.ToString());
                }
                session.SaveChanges();
            }
        }
    }
}