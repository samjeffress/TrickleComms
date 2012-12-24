using NServiceBus;
using SmsMessages.MessageSending.Events;
using SmsTrackingMessages.Messages;

namespace EmailSender
{
    public class EmailService : 
        IHandleMessages<MessageSent>,
        IHandleMessages<MessageFailedSending>,
        IHandleMessages<CoordinatorCompleteEmail>
    {
        public void Handle(MessageSent message)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(MessageFailedSending message)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(CoordinatorCompleteEmail message)
        {
            throw new System.NotImplementedException();
        }
    }
}
