using System.Web.Mvc;
using ConfigurationModels;
using ConfigurationModels.Providers;

namespace SmsWeb.Controllers
{
    public class TwilioConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public PartialViewResult DetailsAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                    return PartialView("_TwilioConfigEdit");
                return PartialView("_TwilioConfigDetails", twilioConfiguration);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                    return PartialView("_TwilioConfigEdit");
                return PartialView("_TwilioConfigEdit", twilioConfiguration);
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(TwilioConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_TwilioConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    session.Store(configuration, "TwilioConfig");
                }
                else
                {
                    twilioConfiguration.AccountSid = configuration.AccountSid;
                    twilioConfiguration.AuthToken = configuration.AuthToken;
                    twilioConfiguration.From = configuration.From;
                }
                session.SaveChanges();
                return PartialView("_TwilioConfigDetails", configuration);
            }
        }
    }
}
