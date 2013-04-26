using System.Linq;
using System.Web.Mvc;
using ConfigurationModels;
using ServiceStack.Common;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class DefaultEmailController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public PartialViewResult DetailsAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                var defaultEmailModel = new DefaultEmailModel();
                if (emailDefaultNotification == null || emailDefaultNotification.EmailAddresses.Count == 0)
                    return PartialView("_DefaultEmailConfigEdit", defaultEmailModel);
                var defaultEmails = emailDefaultNotification.EmailAddresses.Join(", ");
                defaultEmailModel.DefaultEmails = defaultEmails;
                return PartialView("_DefaultEmailConfigDetails", defaultEmailModel);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                var defaultEmailModel = new DefaultEmailModel();
                if (emailDefaultNotification == null || emailDefaultNotification.EmailAddresses.Count == 0)
                    return PartialView("_DefaultEmailConfigEdit", defaultEmailModel);
                var defaultEmails = emailDefaultNotification.EmailAddresses.Join(", ");
                defaultEmailModel.DefaultEmails = defaultEmails;
                return PartialView("_DefaultEmailConfigEdit", defaultEmailModel);
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(DefaultEmailModel configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_DefaultEmailConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var emailAddresses = configuration.DefaultEmails.Split(',');
                var cleanedEmailInList = emailAddresses.ToList().Select(e => e.Trim()).ToList();
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                if (emailDefaultNotification == null)
                {
                    var defaultNotification = new EmailDefaultNotification { EmailAddresses = cleanedEmailInList };
                    session.Store(defaultNotification, "EmailDefaultConfig");
                }
                else
                {
                    emailDefaultNotification.EmailAddresses = cleanedEmailInList;
                }
                session.SaveChanges();
                return PartialView("_DefaultEmailConfigDetails", configuration);
            }
        }
    }
}
