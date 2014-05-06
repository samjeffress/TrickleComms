using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using CsvHelper.Configuration;
using NServiceBus;
using Raven.Json.Linq;
using SmsTrackingModels;
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
            var trickleId = Guid.NewGuid();
            Session.Add("CoordinatorSmsAndEmailModel", model);
            Session.Add("trickleId", trickleId.ToString());
            var hpf = Request.Files[0];
            if (hpf.ContentLength == 0)
                throw new ArgumentException("no content");

            var csvReader = new CsvHelper.CsvReader(new StreamReader(hpf.InputStream), new CsvConfiguration { HasHeaderRecord = false });
            var csvFileContents = new CsvFileContents();
            while (csvReader.Read())
            {
                csvFileContents.Rows.Add(csvReader.CurrentRecord);
            }

            var fileContentsId = trickleId.ToString() + "_fileContents";
            using (var session = Raven.GetStore().OpenSession())
            {
                session.Store(csvFileContents, fileContentsId);
                session.SaveChanges();
            }
            return View("CreateSmsAndEmailPickRows", csvFileContents.Rows[0]);
        }

        [HttpPost]
        public ActionResult CreateSmsAndEmailColumnPicker(FormCollection collection)
        {
            var trickleIdString = Session["trickleId"] as string;
            // TODO: Validate column to data mapping
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

            var fileContentsId = trickleIdString + "_fileContents";
            CsvFileContents fileContents;
            using (var session = Raven.GetStore().OpenSession())
            {
                fileContents = session.Load<CsvFileContents>(fileContentsId);
            }

            var customerContacts = new List<CustomerContact>();

            for (int i = 0; i < fileContents.Rows.Count; i++)
            {
                if (i > 0 || !firstRowIsHeader)
                {
                    var customerContact = new CustomerContact();
                    if (numberPosition >= 0)
                        customerContact.MobileNumber = fileContents.Rows[i][numberPosition];
                    if (emailPosition >= 0)
                        customerContact.EmailAddress = fileContents.Rows[i][emailPosition];
                    if (namePosition >= 0)
                        customerContact.CustomerName = fileContents.Rows[i][namePosition];
                    customerContacts.Add(customerContact);
                }
            }

            //var rowNumber = 0;
            //while (csvReader.Read())
            //{
            //    if (rowNumber > 0 || !firstRowIsHeader)
            //    {
            //        var customerContact = new CustomerContact();
            //        if (numberPosition >= 0)
            //            customerContact.MobileNumber = csvReader.GetField<string>(numberPosition);
            //        if (emailPosition >= 0)
            //            customerContact.EmailAddress = csvReader.GetField<string>(emailPosition);
            //        if (namePosition >= 0)
            //            customerContact.CustomerName = csvReader.GetField<string>(namePosition);
            //        customerContacts.Add(customerContact);
            //    }
            //    rowNumber++;
            //}
            using (var transaction = new TransactionScope())
            {
                var customerContactsId = trickleIdString + "_customerContacts";
                using (var session = Raven.GetStore().OpenSession())
                {
                    session.Store(new CustomerContactList(customerContacts), customerContactsId);
                    session.SaveChanges();
                }
                var message = Mapper.MapToTrickleSmsAndEmailOverPeriod(Guid.Parse(trickleIdString), customerContactsId, originalRequest, CurrentUser.Name());
                Bus.Send("smscoordinator", message);
                transaction.Complete();
            }
            return View("CreateSmsAndEmail");
        }
    }

    public class CsvFileContents
    {
        public CsvFileContents()
        {
            Rows = new List<string[]>();
        }
        public List<string[]> Rows { get; set; }
    }
}
