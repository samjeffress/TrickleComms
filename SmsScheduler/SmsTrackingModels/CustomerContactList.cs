using System.Collections.Generic;

namespace SmsTrackingModels
{
    public class CustomerContactList
    {
        public CustomerContactList()
        {
            CustomerContacts = new List<CustomerContact>();
        }

        public CustomerContactList(List<CustomerContact> customerContacts)
        {
            CustomerContacts = customerContacts;
        }

        public List<CustomerContact> CustomerContacts { get; set; }
    }

    public class CustomerContact
    {
        public string MobileNumber { get; set; }
        public string EmailAddress { get; set; }
        public string CustomerName { get; set; }
    }
}