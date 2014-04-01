using System;
using System.Collections.Generic;
using System.Web;

namespace SmsWeb.Models
{
    public class CoordinatorSmsAndEmailModel : CoordinatorTypeModel
    {
        public List<Contact> Contacts { get; set; }
        public string SmsContent { get; set; }
        public string EmailHtmlContent { get; set; }

        public HttpPostedFileBase FileUpload { get; set; }

        public override Type GetMessageTypeFromModel()
        {
            throw new NotImplementedException();
        }
    }
}