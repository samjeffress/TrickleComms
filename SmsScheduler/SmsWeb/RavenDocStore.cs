using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;

namespace SmsWeb
{
    public interface IRavenDocStore
    {
        IDocumentStore GetStore();
    }

    public class RavenDocStore : IRavenDocStore
    {
        private readonly IDocumentStore _documentStore;
        public RavenDocStore()
        {
            _documentStore = new DocumentStore {Url = "http://localhost:8080"};
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("TwilioConfiguration");
            _documentStore.DatabaseCommands.EnsureDatabaseExists("SmsTracking");
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }
}