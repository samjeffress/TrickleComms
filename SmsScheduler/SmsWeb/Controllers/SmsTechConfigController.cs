using System.Web.Mvc;
using ConfigurationModels;

namespace SmsWeb.Controllers
{
    public class SmsTechConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public PartialViewResult DetailsAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var smsTechConfiguration = session.Load<SmsTechConfiguration>("SmsTechConfig");
                if (smsTechConfiguration == null)
                    return PartialView("_SmsTechConfigEdit");
                return PartialView("_SmsTechConfigDetails", smsTechConfiguration);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var smsTechConfiguration = session.Load<SmsTechConfiguration>("SmsTechConfig");
                if (smsTechConfiguration == null)
                    return PartialView("_SmsTechConfigEdit");
                return PartialView("_SmsTechConfigEdit", smsTechConfiguration);
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(SmsTechConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_SmsTechConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var smsTechConfiguration = session.Load<SmsTechConfiguration>("SmsTechConfig");
                if (smsTechConfiguration == null)
                {
                    session.Store(configuration, "SmsTechConfig");
                }
                else
                {
                    smsTechConfiguration.ApiKey = configuration.ApiKey;
                    smsTechConfiguration.ApiSecret = configuration.ApiSecret;
                    smsTechConfiguration.From = configuration.From;
                }
                session.SaveChanges();
                return PartialView("_SmsTechConfigDetails", configuration);
            }
        }
    }
}
