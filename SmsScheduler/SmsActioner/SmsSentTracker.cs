using NServiceBus;
using SmsMessages.MessageSending.Events;
using SmsMessages.MessageSending.Responses;
using SmsTrackingModels;

namespace SmsActioner
{
    public class SmsSentTracker :
        IHandleMessages<MessageSent>, 
        IHandleMessages<MessageFailedSending>
    {
        public IRavenDocStore RavenStore { get; set; }

        public void Handle(MessageSent message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
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
            using (var session = RavenStore.GetStore().OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var messageSent = session.Load<SmsTrackingData>(message.CorrelationId);
                if (messageSent != null) return;
                session.Store(new SmsTrackingData(message), message.CorrelationId.ToString());
                session.SaveChanges();
            }
        }
    }
}