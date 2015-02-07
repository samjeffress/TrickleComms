using System;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;

namespace SmsWeb.Controllers
{
    public class ReceivedMessageController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public IBus Bus { get; set; }

        public PartialViewResult Count()
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var unacknowledgedSms = session.Query<SmsReceivedData, ReceivedSmsDataByAcknowledgement>().Count(r => r.Acknowledge == false);
                return PartialView("_ReceivedSmsCount", unacknowledgedSms);
            }
        }

        public PartialViewResult Index()
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var unacknowledgedSms = session.Query<SmsReceivedData, ReceivedSmsDataByAcknowledgement>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfNow())
                    .Where(r => r.Acknowledge == false)
                    .ToList();
                return PartialView("_ReceivedSmsIndex", unacknowledgedSms);
            }
        }

        public PartialViewResult Respond(string incomingSmsId)
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var incomingSms = session.Load<SmsReceivedData>(incomingSmsId);
                return PartialView("_ReceivedSmsRespond", incomingSms);
            }
        }

        [HttpPost]
        public PartialViewResult Respond(RespondToSmsIncoming response)
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var incomingSms = session.Load<SmsReceivedData>(response.IncomingSmsId.ToString());
                Bus.Send(new SendOneMessageNow
                {
                    CorrelationId = response.IncomingSmsId,
                    SmsData = new SmsData(incomingSms.SmsData.Mobile, response.Message)
                });
                incomingSms.Acknowledge = true;
                session.SaveChanges();
            }
//            return RedirectToAction("Index"); //should it be this?
             return Index();
        }

        public PartialViewResult Ignore(string incomingSmsId)
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var incomingSms = session.Load<SmsReceivedData>(incomingSmsId);
                incomingSms.Acknowledge = true;
                incomingSms.Ignored = true;
                session.SaveChanges();
            }
            return Index();
        }

    }

    public class RespondToSmsIncoming
    {
        public Guid IncomingSmsId { get; set; }
        public string Message { get; set; }
    }
}