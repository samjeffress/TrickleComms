using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;

namespace EmailSender
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
            _documentStore = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "Configuration" };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("Configuration");
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }
}