using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;

namespace SmsWebTests
{
    public class RavenTestBase
    {
        protected IDocumentStore DocumentStore;

        [SetUp]
        public void Setup()
        {
            DocumentStore = new EmbeddableDocumentStore { RunInMemory = true }.Initialize();
        }

        [TearDown]
        public void Teardown()
        {
            DocumentStore.Dispose();
        }
    }
}