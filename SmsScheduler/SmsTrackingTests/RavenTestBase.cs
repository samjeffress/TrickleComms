using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;

namespace SmsTrackingTests
{
    public class RavenTestBase
    {
        protected IDocumentStore DocumentStore;

        [SetUp]
        public void Setup()
        {
            DocumentStore = new EmbeddableDocumentStore { DefaultDatabase = "SmsTracking", RunInMemory = true }.Initialize();
        }

        [TearDown]
        public void Teardown()
        {
            DocumentStore.Dispose();
        }
    }
}