using System;
using NUnit.Framework;
using SmsTracking;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsSchedulerAuditorTestFixture : RavenTestBase
    {
        [Test]        
        public void HandleMessageScheduled()
        {
            var smsSentAuditor = new SmsSentAuditor { DocumentStore = DocumentStore };
            //smsSentAuditor.Handle(new );
            throw new NotImplementedException();
        }
    }
}