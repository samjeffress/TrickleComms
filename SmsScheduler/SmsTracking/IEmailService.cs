using SmsMessages.MessageSending.Events;

namespace SmsTracking
{
    public interface IEmailService
    {
        void SendSmsSentConfirmation(MessageSent message);
        void SendSmsFailedConfirmation(MessageFailedSending message);
    }

    public class EmailService : IEmailService
    {
        public void SendSmsSentConfirmation(MessageSent message)
        {
            throw new System.NotImplementedException();
        }

        public void SendSmsFailedConfirmation(MessageFailedSending message)
        {
            throw new System.NotImplementedException();
        }
    }
}