using System;
using NServiceBus;
using NServiceBus.Saga;
using SmsActioner.InternalMessages.Commands;
using SmsActioner.InternalMessages.Responses;
using SmsMessages;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Responses;
using SmsMessages.Email.Events;

namespace SmsActioner
{
    public class EmailActioner : Saga<EmailActionerData>,
        IAmStartedByMessages<SendOneEmailNow>, 
//	IHandleMessages<InternalMessages.Responses.EmailSent>,
//        IHandleTimeouts<EmailStatusPendingTimeout>,
        IHandleMessages<SendEmail>,
		IHandleMessages<EmailRecipientEvent>
    {
        public IMailGunWrapper MailGun { get; set; }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<SendEmail>(data => data.Id, message => message.EmailSagaId);
//			ConfigureMapping<InternalMessages.Responses.EmailSent>(data => data.Id, message => message.EmailSagaId);
			ConfigureMapping<EmailRecipientEvent> (data => data.EmailId, message => message.EmailId);
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
//
//		public void Handle(InternalMessages.Responses.EmailSent message)
//        {
//            Data.EmailId = message.EmailId;
////            RequestUtcTimeout<EmailStatusPendingTimeout>(new TimeSpan(0, 2, 0));
//        }

		public void Handle(EmailRecipientEvent message)
		{
			throw new NotImplementedException();
		}

//        public void Timeout(EmailStatusPendingTimeout state)
//        {
//            // TODO : Mailgun Events api with Message-Id as filter
//            var emailStatus = MailGun.CheckStatus(Data.EmailId);
//            var emailStatusUpdate = new EmailStatusUpdate(Data.OriginalMessage, Data.EmailId) { Status = emailStatus };
//            switch (emailStatus)
//            {
//                case EmailStatus.Accepted:
//                    RequestUtcTimeout<EmailStatusPendingTimeout>(new TimeSpan(0,0,2,0));
//                    break;
//                case EmailStatus.Delivered:
//                    if (Data.DeliveredEmailCount == 0)
//                    {
//                        ReplyToOriginator(emailStatusUpdate);
//                        Bus.SendLocal(emailStatusUpdate);
//                        Bus.Publish(new SmsMessages.Email.Events.EmailSent
//                            {
//                                EmailAddress = Data.OriginalMessage.ToAddress,
//                                BodyHtml = Data.OriginalMessage.BodyHtml,
//                                BodyText = Data.OriginalMessage.BodyText,
//                                Subject = Data.OriginalMessage.Subject,
//                                Id = Data.OriginalMessage.CorrelationId,
//                                SendTimeUtc = Data.StartTime.ToUniversalTime()
//                                
//                            });
//                    }
//                    if (Data.DeliveredEmailCount > 10)
//                        // TODO: Should notify originator that there is no more checking
//                        MarkAsComplete();
//                    else
//                        RequestUtcTimeout<EmailStatusPendingTimeout>(new TimeSpan(0, 2, 0, 0));   
//                    Data.DeliveredEmailCount++;
//                    break;
//                case EmailStatus.Failed:
//                case EmailStatus.Clicked:
//                case EmailStatus.Opened:
//                case EmailStatus.Complained:
//                case EmailStatus.Unsubscribed:
//                    ReplyToOriginator(emailStatusUpdate);
//                    Bus.SendLocal(emailStatusUpdate);
//                    MarkAsComplete();
//                    break;
//                default:
//                    throw new NotImplementedException();
//            }
//        }

        public void Handle(SendEmail message)
        {
            var emailId = MailGun.SendEmail(message);
			Data.EmailId = emailId;
//			Bus.Reply(new InternalMessages.Responses.EmailSent { EmailId = emailId, EmailSagaId = message.EmailSagaId });
        }
    }

    public class EmailStatusPendingTimeout
    {
    }

    public class EmailActionerData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual SendOneEmailNow OriginalMessage { get; set; }
        public virtual string EmailId { get; set; }

        public virtual DateTime StartTime { get; set; }

        public virtual int DeliveredEmailCount { get; set; }
    }
}