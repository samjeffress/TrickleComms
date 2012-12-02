using SmsMessages.MessageSending.Events;

namespace SmsTracking
{
    public interface IEmailService
    {
        void SendSmsSentConfirmation(MessageSent message);
        void SendSmsSentConfirmation(MessageFailedSending messageFailedSending);
    }

    public class EmailService : IEmailService
    {
        public void SendSmsSentConfirmation(MessageSent message)
        {
            throw new System.NotImplementedException();
        }

        public void SendSmsSentConfirmation(MessageFailedSending messageFailedSending)
        {
            throw new System.NotImplementedException();
        }
    }
}