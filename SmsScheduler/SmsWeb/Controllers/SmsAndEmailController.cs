using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            return View("CreateSmsAndEmailPickRows", csvFileContents.Rows.Take(5).ToList());
        }

        [HttpPost]
        public ActionResult CreateSmsAndEmailColumnPicker(FormCollection collection)
        {
            var trickleIdString = Session["trickleId"] as string;
            // TODO: Validate column to data mapping
            var firstRowIsHeader = collection["firstRowIsHeader"].Equals("on", StringComparison.CurrentCultureIgnoreCase);
            var columnMapping = GetColumnsForMergeFields(collection);

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
                    if (columnMapping.ContainsKey("Phone"))
                        customerContact.MobileNumber = fileContents.Rows[i][columnMapping["Phone"]];
                    if (columnMapping.ContainsKey("Email"))
                        customerContact.EmailAddress = fileContents.Rows[i][columnMapping["Email"]];
                    if (columnMapping.ContainsKey("CustomerName"))
                        customerContact.CustomerName = fileContents.Rows[i][columnMapping["CustomerName"]];
                    customerContacts.Add(customerContact);
                }
            }

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

        private Dictionary<string, int> GetColumnsForMergeFields(FormCollection collection)
        {
            var mergeFieldColumn = new Dictionary<string, int>();
            
            for (var i = 0; i < collection.Count; i++)
            {
                var a = collection[i].Split('_');
                if (a.Length == 2)
                {
                    if (a[0].Equals("number"))
                        mergeFieldColumn.Add("Phone", Convert.ToInt32(a[1]));
                    if (a[0].Equals("email"))
                        mergeFieldColumn.Add("Email", Convert.ToInt32(a[1]));
                    if (a[0].Equals("name"))
                        mergeFieldColumn.Add("CustomerName", Convert.ToInt32(a[1]));
                }
            }
            return mergeFieldColumn;
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
