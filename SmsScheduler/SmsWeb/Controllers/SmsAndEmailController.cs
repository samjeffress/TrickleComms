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
            var s = collection[0];
            var s1 = collection.Keys[0];
            // todo: use enum instead of string
            var columnMapping = new Dictionary<int, string>();
            for (var i = 0; i < collection.Count; i++)
            {
                var columnMap = collection[i];
                var column = collection.Keys[i];
                if (!string.IsNullOrWhiteSpace(columnMap))
                {
                    columnMapping.Add(Convert.ToInt32(column), columnMap);
                }
            }




            var originalRequest = Session["CoordinatorSmsAndEmailModel"] as CoordinatorSmsAndEmailModel;
            //using (var transaction = new TransactionScope())
            //{
            //    // raven - save document
            //    // nservicebus - send command, let handler parse file and break up items.
            //    using (var session = Raven.GetStore().OpenSession())
            //    {
            //        session.Store(originalRequest.FileUpload, );
            //    }
            //}
            // TODO: Validate column to data mapping
            // TODO: Get previous model from session
            throw new NotImplementedException();
        }
    }
}
