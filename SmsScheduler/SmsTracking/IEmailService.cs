using SmsMessages.MessageSending.Events;
using SmsTrackingMessages.Messages;

namespace SmsTracking
{
    public interface IEmailService
    {
        void SendSmsSentConfirmation(MessageSent message);
        void SendSmsFailedConfirmation(MessageFailedSending message);
        void SendCoordinatorComplete(CoordinatorCompleteEmail message);
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

        public void SendCoordinatorComplete(CoordinatorCompleteEmail message)
        {
            throw new System.NotImplementedException();
        }
    }
}