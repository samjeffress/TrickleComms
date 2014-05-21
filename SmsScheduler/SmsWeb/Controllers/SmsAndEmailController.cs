using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using CsvHelper.Configuration;
using NServiceBus;
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

            var csvFileContents = new CsvFileContents();
            using (var csvReader = new CsvHelper.CsvReader(new StreamReader(hpf.InputStream), new CsvConfiguration { HasHeaderRecord = false }))
            {
                // TODO : Not reading first row properly
                while (csvReader.Read())
                {
                    csvFileContents.Rows.Add(csvReader.CurrentRecord);
                }
            }
            
            var fileContentsId = trickleId.ToString() + "_fileContents";
            using (var session = Raven.GetStore().OpenSession())
            {
                session.Store(csvFileContents, fileContentsId);
                session.SaveChanges();
            }

            var dataColumnPicker = new DataColumnPicker {TrickleId = trickleId, FirstRowIsHeader = true};

            var dropDownList = csvFileContents.CreateSelectList();
            ViewData.Add("selectListData", dropDownList);
            return View("CreateSmsAndEmailPickRows", dataColumnPicker);
        }

        [HttpPost]
        public ActionResult CreateSmsAndEmailColumnPicker(DataColumnPicker model)
        {
            var trickleIdString = Session["trickleId"] as string;
            // TODO: Validate column to data mapping

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
                if (i > 0 || !model.FirstRowIsHeader)
                {
                    var customerContact = new CustomerContact();
                    if (model.PhoneNumberColumn.HasValue)
                        customerContact.MobileNumber = fileContents.Rows[i][model.PhoneNumberColumn.Value];
                    if (model.EmailColumn.HasValue)
                        customerContact.EmailAddress = fileContents.Rows[i][model.EmailColumn.Value];
                    if (model.CustomerNameColumn.HasValue)
                        customerContact.CustomerName = fileContents.Rows[i][model.CustomerNameColumn.Value];
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
    }

    public class CsvFileContents
    {
        public CsvFileContents()
        {
            Rows = new List<string[]>();
        }
        public List<string[]> Rows { get; set; }

        public List<SelectListItem> CreateSelectList()
        {
            var selectListItems = new List<SelectListItem>();
            selectListItems.Add(new SelectListItem { Selected = true, Text = "Not In Data", Value = null});
            var rowDatas = Rows.Take(5).ToList();
            var columnCount = 0;
            if (rowDatas.Count > 0)
                columnCount = rowDatas[0].Count();

            for (int i = 0; i < columnCount; i++)
            {
                var columnValues = rowDatas.Select(r => r[i]).ToList();
                var text = "Column " + i.ToString() + ": " + string.Join(",", columnValues);
                selectListItems.Add(new SelectListItem { Selected = false, Text = text, Value = i.ToString() });
            }
            return selectListItems;
        }
    }
}
