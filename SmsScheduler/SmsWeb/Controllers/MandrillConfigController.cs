using System.Web.Mvc;
using ConfigurationModels;

namespace SmsWeb.Controllers
{
    public class MandrillConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public PartialViewResult DetailsAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mandrillConfiguration = session.Load<MandrillConfiguration>("MandrillConfig");
                if (mandrillConfiguration == null)
                    return PartialView("_MandrillConfigEdit");
                return PartialView("_MandrillConfigDetails", mandrillConfiguration);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mandrillConfiguration = session.Load<MandrillConfiguration>("MandrillConfig");
                if (mandrillConfiguration == null)
                    return PartialView("_MandrillConfigEdit");
                return PartialView("_MandrillConfigEdit", mandrillConfiguration);
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(MandrillConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_MandrillConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mandrillConfiguration = session.Load<MandrillConfiguration>("MandrillConfig");
                if (mandrillConfiguration == null)
                {
                    session.Store(configuration, "MandrillConfig");
                }
                else
                {
                    mandrillConfiguration.ApiKey = configuration.ApiKey;
                    mandrillConfiguration.DefaultFrom = configuration.DefaultFrom;
                    mandrillConfiguration.DomainName = configuration.DomainName;
                }
                session.SaveChanges();
                return PartialView("_MandrillConfigDetails", configuration);
            }
        }
    }
}