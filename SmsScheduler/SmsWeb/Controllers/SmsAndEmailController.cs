using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using ConfigurationModels;
using CsvHelper.Configuration;
using NServiceBus;
using SmsMessages.CommonData;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;
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

            var templateItems = new List<SelectListItem>();
            using (var session = Raven.GetStore().OpenSession("Configuration"))
            {
                templateItems.Add(new SelectListItem { Selected = true, Text = string.Empty, Value = string.Empty});
                var templates = session.Query<CommunicationTemplate>().Take(30).ToList();
                templateItems = templates.Select(c => new SelectListItem { Selected = false, Text = c.TemplateName, Value = c.TemplateName }).ToList();
            }
            ViewData.Add("CommunicationTemplates", templateItems);

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

            if (!string.IsNullOrWhiteSpace(model.TemplateName))
            { 
                using (var session = Raven.GetStore().OpenSession("Configuration"))
                {
                    var communicationTemplate = session.Load<CommunicationTemplate>(model.TemplateName);
                    if (communicationTemplate.TemplateVariables  != null)
                        communicationTemplate.TemplateVariables.ForEach(t => dataColumnPicker.TemplateVariableColumns.Add(t.VariableName, null));
                }
            }

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
            //return View("CreateSmsAndEmail");
            return RedirectToAction("Details", new { coordinatorId = trickleIdString });
        }

        public ActionResult Details(string coordinatorId)
        {
            using (var session = Raven.GetStore().OpenSession())
            {
                var coordinatorSummary = session.Query<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult, ScheduledMessagesStatusCountInCoordinatorIndex>()
                    .Where(s => s.CoordinatorId == coordinatorId)
                    .ToList();
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorId);
                if (coordinatorSummary.Count == 0 || coordinatorTrackingData == null)
                {
                    throw new NotImplementedException("need to have some data...");
                    return View("DetailsNotCreated", model: coordinatorId);
                }

                DateTime? nextSmsDateUtc = session.Query<ScheduleTrackingData, ScheduleMessagesInCoordinatorIndex>()
                                 .Where(s => s.CoordinatorId == Guid.Parse(coordinatorId) && s.MessageStatus == MessageStatus.Scheduled)
                                 .OrderBy(s => s.ScheduleTimeUtc)
                                 .Select(s => s.ScheduleTimeUtc)
                                 .FirstOrDefault();
                if (nextSmsDateUtc == DateTime.MinValue)
                    nextSmsDateUtc = null;

                DateTime? finalSmsDateUtc = session.Query<ScheduleTrackingData, ScheduleMessagesInCoordinatorIndex>()
                    .Where(s => s.CoordinatorId == Guid.Parse(coordinatorId) && s.MessageStatus == MessageStatus.Scheduled)
                    .OrderByDescending(s => s.ScheduleTimeUtc)
                    .Select(s => s.ScheduleTimeUtc)
                    .FirstOrDefault();
                if (finalSmsDateUtc == DateTime.MinValue)
                    finalSmsDateUtc = null;

                var overview = new CoordinatorOverview(coordinatorTrackingData, coordinatorSummary);
                overview.NextScheduledMessageDate = nextSmsDateUtc;
                overview.FinalScheduledMessageDate = finalSmsDateUtc;
                if (HttpContext.Session != null && HttpContext.Session["CoordinatorState_" + coordinatorId] != null && HttpContext.Session["CoordinatorState_" + coordinatorId] is CoordinatorStatusTracking)
                    overview.CurrentStatus = (CoordinatorStatusTracking)HttpContext.Session["CoordinatorState_" + coordinatorId];
                return View("Details", overview);
            }
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
