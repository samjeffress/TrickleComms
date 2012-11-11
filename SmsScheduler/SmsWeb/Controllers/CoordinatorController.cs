using System;
using System.Linq;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class CoordinatorController : Controller
    {
        public IBus Bus { get; set; }

        public ICoordinatorModelToMessageMapping Mapper { get; set; }

        public ActionResult Create()
        {
            return View("Create");
        }

        [HttpPost]
        public ActionResult Create(CoordinatedSharedMessageModel coordinatedMessages)
        {
            var isValid = TryValidateModel(coordinatedMessages);
            if (isValid && SecondaryValidation(coordinatedMessages))
            {
                var coordinatorId = Guid.NewGuid();

                if (coordinatedMessages.TimeSeparator.HasValue && !coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsSpacedByTimePeriod = Mapper.MapToTrickleSpacedByPeriod(coordinatedMessages);
                    trickleSmsSpacedByTimePeriod.CoordinatorId = coordinatorId;
                    Bus.Send(trickleSmsSpacedByTimePeriod);
                }
                if (!coordinatedMessages.TimeSeparator.HasValue && coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsOverTimePeriod = Mapper.MapToTrickleOverPeriod(coordinatedMessages);
                    trickleSmsOverTimePeriod.CoordinatorId = coordinatorId;
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

    public interface ICoordinatorModelToMessageMapping
    {
        TrickleSmsOverTimePeriod MapToTrickleOverPeriod(CoordinatedSharedMessageModel model);

        TrickleSmsSpacedByTimePeriod MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model);
    }

    public class CoordinatorModelToMessageMapping : ICoordinatorModelToMessageMapping
    {
        public TrickleSmsOverTimePeriod MapToTrickleOverPeriod(CoordinatedSharedMessageModel model)
        {
            return new TrickleSmsOverTimePeriod
            {
                Duration = model.SendAllBy.Value.Subtract(model.StartTime),
                Messages =
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                StartTime = model.StartTime,
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic }
            };
        }

        public TrickleSmsSpacedByTimePeriod MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model)
        {
            return new TrickleSmsSpacedByTimePeriod
            {
                Messages =
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                StartTime = model.StartTime,
                TimeSpacing = model.TimeSeparator.Value,
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic }
            };
        }
    }
}
