using System.Web.Mvc;
using ConfigurationModels.Providers;

namespace SmsWeb.Controllers
{
    public class NexmoConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public PartialViewResult DetailsAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var nexmoConfiguration = session.Load<NexmoConfiguration>("NexmoConfig");
                if (nexmoConfiguration == null)
                    return PartialView("_NexmoConfigEdit");
                return PartialView("_NexmoConfigDetails", nexmoConfiguration);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var nexmoConfiguration = session.Load<NexmoConfiguration>("NexmoConfig");
                if (nexmoConfiguration == null)
                    return PartialView("_NexmoConfigEdit");
                return PartialView("_NexmoConfigEdit", nexmoConfiguration);
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(NexmoConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_NexmoConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var nexmoConfiguration = session.Load<NexmoConfiguration>("NexmoConfig");
                if (nexmoConfiguration == null)
                {
                    session.Store(configuration, "NexmoConfig");
                }
                else
                {
                    nexmoConfiguration.ApiKey = configuration.ApiKey;
                    nexmoConfiguration.Secret = configuration.Secret;
                    nexmoConfiguration.From = configuration.From;
                }
                session.SaveChanges();
                return PartialView("_NexmoConfigDetails", configuration);
            }
        }
    }
}
