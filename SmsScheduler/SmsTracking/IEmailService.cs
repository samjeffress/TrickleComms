using SmsMessages.MessageSending;

namespace SmsTracking
{
    public interface IEmailService
    {
        void SendSmsSentConfirmation(MessageSent message);
    }

    public class EmailService : IEmailService
    {
        public void SendSmsSentConfirmation(MessageSent message)
        {
            throw new System.NotImplementedException();
        }
    }
}