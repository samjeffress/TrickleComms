using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ConfigurationModels;

namespace SmsWeb.Controllers
{
    public class ProviderConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public PartialViewResult EditSMSAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var smsProviderConfiguration = session.Load<SmsProviderConfiguration>("SmsProviderConfiguration");
                if (smsProviderConfiguration == null)
                {
                    ViewData.Add("SmsProviders", SelectListForEnum<SmsProvider>(string.Empty));
                    return PartialView("_SmsProviderConfigEdit");
                }
                var selectedProviderString = smsProviderConfiguration.SmsProvider.ToString();
                ViewData.Add("SmsProviders", SelectListForEnum<SmsProvider>(selectedProviderString));
                return PartialView("_SmsProviderConfigEdit", smsProviderConfiguration);
            }
        }

        private static List<SelectListItem> SelectListForEnum<T>(string selectValue)
        {
            var providerSelectList = new List<SelectListItem>();
            providerSelectList.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });

            var enumValues = Enum.GetValues(typeof(T)).Cast<T>();
            providerSelectList.AddRange(enumValues.Select(p => new SelectListItem { Selected = false, Text = p.ToString(), Value = p.ToString() }).ToList());
            var activeItem = providerSelectList.First(p => p.Value == selectValue);
            activeItem.Selected = true;
            return providerSelectList;
        }
            
        [HttpPost]
        public PartialViewResult EditSMSAjax(SmsProviderConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_SmsProviderConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var providerConfiguration = session.Load<SmsProviderConfiguration>("SmsProviderConfiguration");
                if (providerConfiguration == null)
                {
                    session.Store(configuration, "SmsProviderConfiguration");
                }
                else
                {
                    providerConfiguration.SmsProvider = configuration.SmsProvider;
                }
                session.SaveChanges();
                ViewData.Add("SmsProviders", SelectListForEnum<SmsProvider>(configuration.SmsProvider.ToString()));
                return PartialView("_SmsProviderConfigEdit", configuration);
            }
        }

        public PartialViewResult EditEmailAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var emailProviderConfiguration = session.Load<EmailProviderConfiguration>("EmailProviderConfiguration");
                if (emailProviderConfiguration == null)
                {
                    ViewData.Add("EmailProviders", SelectListForEnum<EmailProvider>(string.Empty));
                    return PartialView("_EmailProviderConfigEdit");
                }
                var selectedProviderString = emailProviderConfiguration.EmailProvider.ToString();
                ViewData.Add("EmailProviders", SelectListForEnum<EmailProvider>(selectedProviderString));
                return PartialView("_EmailProviderConfigEdit", emailProviderConfiguration);
            }
        }

        [HttpPost]
        public PartialViewResult EditEmailAjax(EmailProviderConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_EmailProviderConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var providerConfiguration = session.Load<EmailProviderConfiguration>("EmailProviderConfiguration");
                if (providerConfiguration == null)
                {
                    session.Store(configuration, "EmailProviderConfiguration");
                }
                else
                {
                    providerConfiguration.EmailProvider = configuration.EmailProvider;
                }
                session.SaveChanges();
                ViewData.Add("EmailProviders", SelectListForEnum<EmailProvider>(configuration.EmailProvider.ToString()));
                return PartialView("_EmailProviderConfigEdit", configuration);
            }
        }
    }
}
