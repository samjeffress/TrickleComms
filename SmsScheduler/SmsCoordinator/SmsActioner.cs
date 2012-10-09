using NServiceBus;
using SmsMessages.Commands;
using SmsMessages.Events;

namespace SmsCoordinator
{
    public class SmsActioner : IHandleMessages<SendOneMessageNow>
    {
        public ISmsService SmsService { get; set; }
        public IBus Bus { get; set; }

        public void Handle(SendOneMessageNow sendOneMessageNow)
        {
            var receipt = SmsService.Send(sendOneMessageNow);
            Bus.Publish<MessageSent>(m =>
            {
                m.Receipt = receipt;
                m.CorrelationId = sendOneMessageNow.CorrelationId;
                m.SmsData = sendOneMessageNow.SmsData;
                m.SmsMetaData = sendOneMessageNow.SmsMetaData;
            });
        }
    }
}