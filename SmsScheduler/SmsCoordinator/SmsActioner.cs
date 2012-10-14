using NServiceBus;
using SmsMessages.MessageSending;

namespace SmsCoordinator
{
    public class SmsActioner : IHandleMessages<SendOneMessageNow>
    {
        public ISmsService SmsService { get; set; }
        public IBus Bus { get; set; }

        public void Handle(SendOneMessageNow sendOneMessageNow)
        {
            var confirmationData = SmsService.Send(sendOneMessageNow);
            Bus.Publish<MessageSent>(m =>
            {
                m.ConfirmationData = confirmationData;
                m.CorrelationId = sendOneMessageNow.CorrelationId;
                m.SmsData = sendOneMessageNow.SmsData;
                m.SmsMetaData = sendOneMessageNow.SmsMetaData;
            });
        }
    }
}