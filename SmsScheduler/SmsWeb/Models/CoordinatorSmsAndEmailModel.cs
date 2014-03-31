using System;
using System.Collections.Generic;

namespace SmsWeb.Models
{
    public class CoordinatorSmsAndEmailModel : CoordinatorTypeModel
    {
        public List<Contact> Contacts { get; set; }
        public string SmsContent { get; set; }
        public string EmailHtmlContent { get; set; }

        public override Type GetMessageTypeFromModel()
        {
            throw new NotImplementedException();
        }
    }
}