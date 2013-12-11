using System.Linq;
using System.Web.Mvc;
using SmsTrackingModels;

namespace SmsWeb.Controllers
{
    public class ReceivedMessageController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

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
                    .Where(r => r.Acknowledge == false)
                    .ToList();
                return PartialView("_ReceivedSmsIndex", unacknowledgedSms);
            }
        }

    }
}