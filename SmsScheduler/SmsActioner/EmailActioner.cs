using System;
using NServiceBus;
using NServiceBus.Saga;
using SmsActioner.InternalMessages.Commands;
using SmsActioner.InternalMessages.Responses;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Responses;

namespace SmsActioner
{
    public class EmailActioner : Saga<EmailActionerData>,
        IAmStartedByMessages<SendOneEmailNow>,
        IHandleMessages<EmailSent>,
        IHandleTimeouts<EmailStatusPendingTimeout>,
        IHandleMessages<SendEmail>
    {
        public IMailGunWrapper MailGun { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<EmailSent>(data => data.Id, message => message.EmailSagaId);
            base.ConfigureHowToFindSaga();
        }

        public void Handle(SendOneEmailNow message)
        {
            Data.OriginalMessage = message;
            Data.StartTime = DateTime.Now;
            Bus.SendLocal(new SendEmail
                {
                    EmailSagaId = Data.Id,
                    BaseRequest = message
                });
        }

        public void Handle(EmailSent message)
        {
            Data.EmailId = message.EmailId;
            RequestTimeout<EmailStatusPendingTimeout>(new TimeSpan(0, 20, 0));
        }

        public void Timeout(EmailStatusPendingTimeout state)
        {
            // TODO : Mailgun Events api with Message-Id as filter
            // TODO : Figure out what we do about usage
            // TODO: Figure out when we should stop checking the status (is opened enough - should we wait 24 hours to see if they click?)
            var emailStatus = MailGun.CheckStatus(Data.EmailId);
            switch (emailStatus)
            {
                case EmailStatus.Accepted:
                    RequestTimeout<EmailStatusPendingTimeout>(new TimeSpan(0,0,20,0));
                    break;
                case EmailStatus.Delivered:
                    if (Data.DeliveredEmailCount == 0)
                    {
                        var emailDelivered = new EmailDelivered { CorrelationId = Data.OriginalMessage.CorrelationId, EmailId = Data.EmailId };
                        ReplyToOriginator(emailDelivered);
                        Bus.SendLocal(emailDelivered);
                    }
                    if (Data.DeliveredEmailCount > 10)
                    {
                        MarkAsComplete();
                    }
                    else
                    {
                        RequestTimeout<EmailStatusPendingTimeout>(new TimeSpan(0, 2, 0, 0));   
                    }
                    Data.DeliveredEmailCount++;
                    break;
                case EmailStatus.Failed:
                    var emailDeliveryFailed = new EmailDeliveryFailed {CorrelationId = Data.OriginalMessage.CorrelationId, EmailId = Data.EmailId};
                    ReplyToOriginator(emailDeliveryFailed);
                    Bus.SendLocal(emailDeliveryFailed);
                    MarkAsComplete();
                    break;
                case EmailStatus.Clicked:
                    var emailDeliveredAndClicked = new EmailDeliveredAndClicked { CorrelationId = Data.OriginalMessage.CorrelationId, EmailId = Data.EmailId };
                    ReplyToOriginator(emailDeliveredAndClicked);
                    Bus.SendLocal(emailDeliveredAndClicked);
                    MarkAsComplete();
                    break;
                case EmailStatus.Opened:
                    var emailDeliveredAndOpened = new EmailDeliveredAndOpened { CorrelationId = Data.OriginalMessage.CorrelationId, EmailId = Data.EmailId };
                    ReplyToOriginator(emailDeliveredAndOpened);
                    Bus.SendLocal(emailDeliveredAndOpened);
                    MarkAsComplete();
                    break;
                case EmailStatus.Complained:
                    var emailComplained = new EmailComplained {CorrelationId = Data.OriginalMessage.CorrelationId, EmailId = Data.EmailId};
                    ReplyToOriginator(emailComplained);
                    Bus.SendLocal(emailComplained);
                    MarkAsComplete();
                    break;
                case EmailStatus.Unsubscribed:
                    var emailUnsubscribed = new EmailUnsubscribed { CorrelationId = Data.OriginalMessage.CorrelationId, EmailId = Data.EmailId };
                    ReplyToOriginator(emailUnsubscribed);
                    Bus.SendLocal(emailUnsubscribed);
                    MarkAsComplete();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Handle(SendEmail message)
        {
            var emailId = MailGun.SendEmail(message);
            Bus.Reply(new EmailSent { EmailId = emailId, EmailSagaId = message.EmailSagaId });
        }
    }

    public class EmailStatusPendingTimeout
    {
    }

    public class EmailActionerData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public SendOneEmailNow OriginalMessage { get; set; }
        public string EmailId { get; set; }

        public DateTime StartTime { get; set; }

        public int DeliveredEmailCount { get; set; }
    }
}