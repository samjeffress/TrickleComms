using System.Web.Mvc;
using ConfigurationModels;
using ConfigurationModels.Providers;

namespace SmsWeb.Controllers
{
    public class MailgunConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public PartialViewResult DetailsAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                    return PartialView("_MailgunConfigEdit");
                return PartialView("_MailgunConfigDetails", mailgunConfiguration);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                    return PartialView("_MailgunConfigEdit");
                return PartialView("_MailgunConfigEdit", mailgunConfiguration);
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(MailgunConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_MailgunConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                {
                    session.Store(configuration, "MailgunConfig");
                }
                else
                {
                    mailgunConfiguration.ApiKey = configuration.ApiKey;
                    mailgunConfiguration.DefaultFrom = configuration.DefaultFrom;
                    mailgunConfiguration.DomainName = configuration.DomainName;
                }
                session.SaveChanges();
                return PartialView("_MailgunConfigDetails", configuration);
            }
        }
    }
}