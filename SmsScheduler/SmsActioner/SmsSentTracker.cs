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
                var messageSent = session.Load<SmsTrackingData>(message.CorrelationId.ToString());
                if (messageSent != null) return;
                session.Store(new SmsTrackingData(message), message.CorrelationId.ToString());
                session.SaveChanges();
            }
        }
    }
}