using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using NServiceBus;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class SmsAndEmailController : Controller
    {
        public IBus Bus { get; set; }
        public IRavenDocStore Raven { get; set; }
        public ICoordinatorModelToMessageMapping Mapper { get; set; }
        public ICurrentUser CurrentUser { get; set; }
        public ActionResult Create()
        {
            return View("CreateSmsAndEmail");
        }

        [HttpPost]
        public ActionResult Create(CoordinatorSmsAndEmailModel model)
        {
            // shouldn't stuff the csv into session
            Session.Add("CoordinatorSmsAndEmailModel", model);
            foreach (string file in Request.Files)
            {
                HttpPostedFileBase hpf = Request.Files[file] as HttpPostedFileBase;
                if (hpf.ContentLength == 0)
                    continue;
                string savedFileName = Path.Combine(
                   AppDomain.CurrentDomain.BaseDirectory,
                   Path.GetFileName(hpf.FileName));
                hpf.SaveAs(savedFileName);
            }
            var csvParser = new CsvHelper.CsvParser(new StreamReader(model.FileUpload.InputStream));
            var firstRow = csvParser.Read();
            return View("CreateSmsAndEmailPickRows", firstRow);
        }

        [HttpPost]
        public ActionResult CreateSmsAndEmailColumnPicker(FormCollection collection)
        {
            // TODO: Validate column to data mapping
            
            var trickleId = Guid.NewGuid();
            var numberPosition = -1;
            var emailPosition = -1;
            var namePosition = -1;
            var firstRowIsHeader = false;

            for (var i = 0; i < collection.Count; i++)
            {
                var a = collection[i].Split('_');
                if (a.Length == 2)
                {
                    if (a[0].Equals("number"))
                        numberPosition = Convert.ToInt32(a[1]);
                    if (a[0].Equals("email"))
                        emailPosition = Convert.ToInt32(a[1]);
                    if (a[0].Equals("name"))
                        namePosition = Convert.ToInt32(a[1]);
                }
                else
                {
                    if (collection.Keys[i].Equals("firstRowIsHeader"))
                        if (collection[i].Equals("on", StringComparison.CurrentCultureIgnoreCase))
                            firstRowIsHeader = true;
                }
            }

            var originalRequest = Session["CoordinatorSmsAndEmailModel"] as CoordinatorSmsAndEmailModel;

            var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(originalRequest.FileUpload.FileName));
            var csvParser = new CsvHelper.CsvParser(new StreamReader(csvPath));

            var customerContacts = new List<CustomerContact>();
            var rowNumber = 0;
            while (true)
            {
                var row = csvParser.Read();
                if (row == null)
                    break;
                if (rowNumber > 0 || !firstRowIsHeader)
                {
                    var customerContact = new CustomerContact();
                    if (numberPosition >= 0)
                        customerContact.MobileNumber = row[numberPosition];
                    if (emailPosition >= 0)
                        customerContact.EmailAddress = row[emailPosition];
                    if (namePosition >= 0)
                        customerContact.CustomerName = row[namePosition];
                    customerContacts.Add(customerContact);
                }
                rowNumber++;
            }
            using (var transaction = new TransactionScope())
            {
                using (var session = Raven.GetStore().OpenSession())
                {
                    session.Store(new CustomerContactList(customerContacts), trickleId + "_customerContacts");
                    session.SaveChanges();
                }
                var message = Mapper.MapToTrickleSmsAndEmailOverPeriod(originalRequest, CurrentUser.Name());
                Bus.Send("smscoordinator", message);
                transaction.Complete();
            }
            return View("CreateSmsAndEmail");
        }
    }

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
