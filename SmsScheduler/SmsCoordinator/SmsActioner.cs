using System;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;

namespace SmsCoordinator
{
    public class SmsActioner : Saga<SmsActionerData>, 
        IAmStartedByMessages<SendOneMessageNow>,
        IHandleTimeouts<SmsPendingTimeout>
    {
        public ISmsService SmsService { get; set; }

        public ITwilioWrapper TwilioWrapper { get; set; }

        public void Handle(SendOneMessageNow sendOneMessageNow)
        {
            Data.OriginalMessage = sendOneMessageNow;
            var confirmationData = SmsService.Send(sendOneMessageNow);
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
            if (confirmationData is SmsSent)
            {
                var sentMessage = confirmationData as SmsSent;
                Bus.Publish<MessageSent>(m =>
                {
                    m.ConfirmationData = sentMessage.SmsConfirmationData;
                    m.CorrelationId = Data.OriginalMessage.CorrelationId;
                    m.SmsData = Data.OriginalMessage.SmsData;
                    m.SmsMetaData = Data.OriginalMessage.SmsMetaData;
                    m.ConfirmationEmailAddress = Data.OriginalMessage.ConfirmationEmailAddress;
                });
                MarkAsComplete();
            }
            else
            {
                var dateTime = DateTime.UtcNow.AddMinutes(1);
                RequestUtcTimeout<SmsPendingTimeout>(dateTime);
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
    }

    public class SmsPendingTimeout
    {
    }
}