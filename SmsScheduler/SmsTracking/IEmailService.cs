using SmsMessages.MessageSending;

namespace SmsTracking
{
    public interface IEmailService
    {
        void SendSmsSentConfirmation(MessageSent message);
    }
}