using System.Web.Mvc;

namespace SmsWeb.Models
{
    public class TemplateModel
    {
        public string TemplateName { get; set; }
        [AllowHtml]
        public string EmailContent { get; set; }
        public string SmsContent { get; set; }
    }
}