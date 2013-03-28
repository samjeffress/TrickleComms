﻿using System.Linq;
using System.Web.Mvc;

namespace SmsWeb.Controllers
{
    public class TagController : Controller
    {

        public IRavenDocStore RavenDocStore { get; set; }

        public JsonResult Index()
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var reduceResults = session.Query<CoordinatorTagList.ReduceResult, CoordinatorTagList>().ToList();
                return Json(reduceResults, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Search(string query)
        {
            return View();
        }
    }
}