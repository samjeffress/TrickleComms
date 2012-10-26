using System.Web.Mvc;
using NUnit.Framework;
using SmsWeb.Controllers;
using SmsWeb.Models;

namespace SmsWebTests
{
    [TestFixture]
    public class SendNowTestFixture
    {
        [Test]
        public void SendNowInvalidReturnsToCreatePageWithValidation()
        {
            var controller = new SendNowController();
            var sendNowModel = new SendNowModel();
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }
    }
}
