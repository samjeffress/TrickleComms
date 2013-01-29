using System.Collections.Generic;
using System.Web.Mvc;
using ConfigurationModels;
using ServiceStack.Common;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class DefaultEmailController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public ActionResult Details()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                if (emailDefaultNotification == null)
                    return RedirectToAction("Edit");
                var defaultEmailModel = new DefaultEmailModel {DefaultEmails = emailDefaultNotification.EmailAddresses.Join(", ")};
                return View("Details", defaultEmailModel);
            }
        }

        public ActionResult Edit()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                var defaultEmailModel = new DefaultEmailModel();
                if (emailDefaultNotification == null || emailDefaultNotification.EmailAddresses.Count == 0)
                    return View("Edit", defaultEmailModel);
                var defaultEmails = emailDefaultNotification.EmailAddresses.Join(", ");
                defaultEmailModel.DefaultEmails = defaultEmails;
                return View("Edit", defaultEmailModel);
            }
        }

        [HttpPost]
        public ActionResult Edit(DefaultEmailModel configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return View("Edit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var emailAddresses = configuration.DefaultEmails.Split(',');
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                if (emailDefaultNotification == null)
                {
                    var defaultNotification = new EmailDefaultNotification {EmailAddresses = new List<string>(emailAddresses)};
                    session.Store(defaultNotification, "EmailDefaultConfig");
                }
                else
                {
                    emailDefaultNotification.EmailAddresses = new List<string>(emailAddresses);
                }
                session.SaveChanges();
                return RedirectToAction("Details");
            }
        }
    }
}
