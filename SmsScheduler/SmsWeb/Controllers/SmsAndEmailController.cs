using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
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
        public ActionResult Create()
        {
            return View("CreateSmsAndEmail");
        }

        [HttpPost]
        public ActionResult Create(CoordinatorSmsAndEmailModel model)
        {
            // shouldn't stuff the csv into session
            Session.Add("CoordinatorSmsAndEmailModel", model);

            var csvParser = new CsvHelper.CsvParser(new StreamReader(model.FileUpload.InputStream));
            var firstRow = csvParser.Read();
            return View("CreateSmsAndEmailPickRows", firstRow);
        }

        [HttpPost]
        public ActionResult CreateSmsAndEmailColumnPicker(FormCollection collection)
        {
            var trickleId = Guid.NewGuid();
            var numberPosition = -1;
            var emailPosition = -1;
            var namePosition = -1;

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
            }

            var originalRequest = Session["CoordinatorSmsAndEmailModel"] as CoordinatorSmsAndEmailModel;
            var csvParser = new CsvHelper.CsvParser(new StreamReader(originalRequest.FileUpload.InputStream));

            var customerContacts = new List<CustomerContact>();

            while (true)
            {
                var row = csvParser.Read();
                if (row == null)
                    break;

                var customerContact = new CustomerContact();
                if (numberPosition >= 0)
                    customerContact.MobileNumber = row[numberPosition];
                if (emailPosition >= 0)
                    customerContact.EmailAddress = row[emailPosition];
                if (namePosition >= 0)
                    customerContact.CustomerName = row[namePosition];
                customerContacts.Add(customerContact);
            }

            using (var transaction = new TransactionScope())
            {
                // nservicebus - send command, let handler parse file and break up items.
                using (var session = Raven.GetStore().OpenSession())
                {
                    session.Store(customerContacts, trickleId + "_customerContacts");
                }
                Mapper.ma
                Bus.Send()

                transaction.Complete();
            }
            // TODO: Validate column to data mapping
            // TODO: Get previous model from session
            throw new NotImplementedException();
        }
    }

    public class CustomerContact
    {
        public string MobileNumber { get; set; }
        public string EmailAddress { get; set; }
        public string CustomerName { get; set; }
    }
}
