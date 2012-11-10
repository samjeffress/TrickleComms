using System;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.Coordinator;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class CoordinatorController : Controller
    {
        public IBus Bus { get; set; }

        [HttpPost]
        public ActionResult Create(CoordinatedSharedMessageModel coordinatedMessages)
        {

            var isValid = TryValidateModel(coordinatedMessages);
            if (isValid && SecondaryValidation(coordinatedMessages))
            {
                var coordinatorId = Guid.NewGuid();

                if (coordinatedMessages.TimeSeparator.HasValue && !coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsSpacedByTimePeriod = new TrickleSmsSpacedByTimePeriod();
                    Bus.Send(trickleSmsSpacedByTimePeriod);
                }
                if (!coordinatedMessages.TimeSeparator.HasValue && coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsOverTimePeriod = new TrickleSmsOverTimePeriod();
                    Bus.Send(trickleSmsOverTimePeriod);    
                }


                return RedirectToAction("Details", "Coordinator", new {coordinatorId = coordinatorId.ToString()});
            }
            return View("Create", coordinatedMessages);
        }

        private bool SecondaryValidation(CoordinatedSharedMessageModel coordinatedMessages)
        {
            if (coordinatedMessages.StartTime < DateTime.Now)
                return false;
            if (coordinatedMessages.SendAllBy.HasValue && coordinatedMessages.SendAllBy.Value <= coordinatedMessages.StartTime)
                return false;
            if (coordinatedMessages.SendAllBy.HasValue && coordinatedMessages.TimeSeparator.HasValue)
                return false;
            if (!coordinatedMessages.SendAllBy.HasValue && !coordinatedMessages.TimeSeparator.HasValue)
                return false;
            return true;
        }

        public ActionResult Details(string coordinatorid)
        {
            throw new NotImplementedException();
        }
    }
}
