using System;
using System.Linq;
using System.Net.Mail;
using System.Text;
using ConfigurationModels;
using ConfigurationModels.Providers;
using NServiceBus;
using SmsMessages.Coordinator.Events;
using SmsMessages.Email.Commands;

namespace SmsCoordinator.Email
{
    public class EmailService : 
        IHandleMessages<CoordinatorCompleteEmailWithSummary>,
        IHandleMessages<CoordinatorCreatedEmail>
    {
        public IRavenDocStore RavenDocStore { get; set; }
        public IMailActioner MailActioner { get; set; }
        public IDateTimeOlsenFromUtcMapping DateTimeOlsenFromUtcMapping { get; set; }

        public void Handle(CoordinatorCreatedEmail created)
        {
            var message = created.CoordinatorCreated;
            using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                if (message.ConfirmationEmailAddresses.Count == 0 && (emailDefaultNotification == null || emailDefaultNotification.EmailAddresses.Count == 0))
                    return;
                
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null || string.IsNullOrWhiteSpace(mailgunConfiguration.DefaultFrom))
                    throw new ArgumentException("Could not find the default 'From' sender.");
                var subject = "Coordinator " + message.MetaData.Topic + " (" + message.CoordinatorId + ") created.";

                var creationDateUserZone = DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(message.CreationDateUtc, message.UserOlsenTimeZone);

                var startTimeUserZone =
                    DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(
                        message.ScheduledMessages.Select(s => s.ScheduledTimeUtc).Min(), message.UserOlsenTimeZone);

                var endTimeUserZone =
                    DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(
                        message.ScheduledMessages.Select(s => s.ScheduledTimeUtc).Max(), message.UserOlsenTimeZone);

                var body = EmailTemplateResolver.GetEmailBody(@"Email\Templates\CoordinatorCreated.cshtml", new
                    {
                        CoordinatorId = message.CoordinatorId,
                        CreationDateUserZone = creationDateUserZone,
                        MessageCount = message.ScheduledMessages.Count,
                        StartTimeUserZone = startTimeUserZone,
                        EndTimeUserZone = endTimeUserZone,
                        UserTimeZone = message.UserOlsenTimeZone,
                        Topic = message.MetaData.Topic
                    });

                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailgunConfiguration.DefaultFrom); 
                mailMessage.Body = body;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = subject;

                foreach (var emailAddress in message.ConfirmationEmailAddresses)
                {
                    mailMessage.To.Add(emailAddress);
                }

                if (emailDefaultNotification != null)
                    emailDefaultNotification.EmailAddresses.ForEach(e => mailMessage.To.Add(e));
                MailActioner.Send(mailgunConfiguration, mailMessage);
            }
        }

        public void Handle(CoordinatorCompleteEmailWithSummary message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                if (message.EmailAddresses.Count == 0 && (emailDefaultNotification == null || emailDefaultNotification.EmailAddresses.Count == 0))
                    return;

                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null || string.IsNullOrWhiteSpace(mailgunConfiguration.DefaultFrom))
                    throw new ArgumentException("Could not find the default 'From' sender.");
                var subject = "Coordinator " + message.Topic + " (" + message.CoordinatorId + ") complete.";

                var finishTimeUserZone = DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(message.FinishTimeUtc, message.UserOlsenTimeZone);

                var body = EmailTemplateResolver.GetEmailBody(@"Email\Templates\CoordinatorFinishedWithSummary.cshtml", new
                {
                    message.CoordinatorId,
                    FinishTimeUserZone = finishTimeUserZone,
                    UserTimeZone = message.UserOlsenTimeZone,
                    MessageCount = message.SuccessCount + message.FailedCount,
                    SuccessfulMessageCount = message.SuccessCount,
                    UnsuccessfulMessageCount = message.FailedCount,
                    TotalCost = message.Cost,
                    Topic = message.Topic
                });

                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailgunConfiguration.DefaultFrom);
                mailMessage.Body = body;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = subject;
                foreach (var emailAddress in message.EmailAddresses)
                {
                    mailMessage.To.Add(emailAddress);
                }
                if (emailDefaultNotification != null)
                    emailDefaultNotification.EmailAddresses.ForEach(e => mailMessage.To.Add(e));
                MailActioner.Send(mailgunConfiguration, mailMessage);
            }
        }
    }
}
