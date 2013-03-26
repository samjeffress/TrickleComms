using System.Collections.Generic;
using System.Web.Mvc;

namespace SmsWeb.Controllers
{
    public class TagsController : Controller
    {
        public JsonResult TagList()
        {
            var keyValuePairs = new List<TagInfo>
            {
                new TagInfo { Id = "one", Name = "one" }, 
                new TagInfo { Id = "two", Name = "two" }
            };
            return Json(keyValuePairs, JsonRequestBehavior.AllowGet);
        }
    }

    public class TagInfo
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}
