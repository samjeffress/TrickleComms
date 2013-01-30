using System.Web.Mvc;
using ConfigurationModels;

namespace SmsWeb.Controllers
{
    public class CountryCodeConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public ActionResult Index()
        {
            return View();
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var countryCode = session.Load<CountryCodeReplacement>("CountryCodeConfig");
                if (countryCode == null)
                    return PartialView("_CountryCodeConfigCreate");
                return PartialView("_CountryCodeConfigCreate", countryCode);
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(CountryCodeReplacement configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_CountryCodeConfigCreate", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var countryCode = session.Load<CountryCodeReplacement>("CountryCodeConfig");
                if (countryCode == null)
                {
                    session.Store(configuration, "CountryCodeConfig");
                }
                else
                {
                    countryCode.CountryCode = configuration.CountryCode;
                    countryCode.LeadingNumberToReplace = configuration.LeadingNumberToReplace;
                }
                session.SaveChanges();
                return PartialView("_CountryCodeConfigDetails", configuration);
            }
        }
    }
}
