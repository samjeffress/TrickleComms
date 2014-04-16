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
            var emailStatus = MailGun.CheckStatus(Data.EmailId);
            if (emailStatus == EmailStatus.Opened)
                ReplyToOriginator(new EmailSuccessfullyDelivered());
            else
                throw new NotImplementedException();
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
    }
}