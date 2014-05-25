using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Saga;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;
using SmsMessages.MessageSending.Responses;

namespace SmsActioner
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
                var messageFailedSending = new MessageFailedSending
                    {
                        SmsFailed = failedMessage,
                        CorrelationId = Data.OriginalMessage.CorrelationId,
                        SmsData = Data.OriginalMessage.SmsData,
                        SmsMetaData = Data.OriginalMessage.SmsMetaData,
                        ConfirmationEmailAddress = Data.OriginalMessage.ConfirmationEmailAddress
                    };
                ReplyToOriginator(messageFailedSending);
                Bus.SendLocal(messageFailedSending);
                MarkAsComplete();
            }
            else if (confirmationData is SmsSent)
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
                var messageSuccessfullyDelivered = new MessageSuccessfullyDelivered
                    {
                        ConfirmationData = sentMessage.SmsConfirmationData, CorrelationId = Data.OriginalMessage.CorrelationId, SmsData = Data.OriginalMessage.SmsData, SmsMetaData = Data.OriginalMessage.SmsMetaData, ConfirmationEmailAddress = Data.OriginalMessage.ConfirmationEmailAddress
                    };
                Bus.SendLocal(messageSuccessfullyDelivered);
                ReplyToOriginator(messageSuccessfullyDelivered);
                MarkAsComplete();
            }
            else
            {
                RequestUtcTimeout<SmsPendingTimeout>(new TimeSpan(0, 0, 0, 10));
            }
        }
    }

    public class SmsActionerData : IContainSagaData
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
