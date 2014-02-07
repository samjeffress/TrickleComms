using System;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;

namespace SmsActioner
{
    public class SmsActioner : Saga<SmsActionerData>, 
        IAmStartedByMessages<SendOneMessageNow>,
        IHandleTimeouts<SmsPendingTimeout>
    {
        public ISmsService SmsService { get; set; }

        public void Handle(SendOneMessageNow sendOneMessageNow)
        {
            var confirmationData = SmsService.Send(sendOneMessageNow);
            if (confirmationData is SmsSent)
                throw new ArgumentException("SmsSent type is invalid - followup is required to get delivery status");
            if (confirmationData is SmsQueued)
                throw new ArgumentException("SmsQueued type is invalid - followup is required to get delivery status");
            Data.OriginalMessage = sendOneMessageNow;
            Data.SmsRequestId = confirmationData.Sid;
            ProcessConfirmationData(confirmationData);
        }

        public void Timeout(SmsPendingTimeout state)
        {
            var smsStatus = SmsService.CheckStatus(Data.SmsRequestId);
            ProcessConfirmationData(smsStatus);
        }

        private void ProcessConfirmationData(SmsStatus confirmationData)
        {
            if (confirmationData is SmsFailed)
            {
                var failedMessage = confirmationData as SmsFailed;
                Bus.Publish<MessageFailedSending>(m =>
                {
                    m.SmsFailed = failedMessage;
                    m.CorrelationId = Data.OriginalMessage.CorrelationId;
                    m.SmsData = Data.OriginalMessage.SmsData;
                    m.SmsMetaData = Data.OriginalMessage.SmsMetaData;
                    m.ConfirmationEmailAddress = Data.OriginalMessage.ConfirmationEmailAddress;
                });
                MarkAsComplete();
            }
            else if (confirmationData is SmsSent)
            {
                var sentMessage = confirmationData as SmsSent;
                Bus.Publish<MessageSent>(m =>
                {
                    m.ConfirmationData = new SmsConfirmationData(Data.SmsRequestId, sentMessage.SmsConfirmationData.SentAtUtc, Data.Price);
                    m.CorrelationId = Data.OriginalMessage.CorrelationId;
                    m.SmsData = Data.OriginalMessage.SmsData;
                    m.SmsMetaData = Data.OriginalMessage.SmsMetaData;
                    m.ConfirmationEmailAddress = Data.OriginalMessage.ConfirmationEmailAddress;
                });
                MarkAsComplete();
            }
            else
            {
                if (confirmationData is SmsSending)
                    Data.Price = (confirmationData as SmsSending).Price;
                RequestUtcTimeout<SmsPendingTimeout>(new TimeSpan(0, 0, 0, 10));
            }
        }
    }

    public class SmsActionerData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string SmsRequestId { get; set; }

        public SendOneMessageNow OriginalMessage { get; set; }

        public decimal Price { get; set; }
    }

    public class SmsPendingTimeout
    {
    }
}