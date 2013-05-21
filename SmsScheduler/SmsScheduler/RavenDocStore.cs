using System;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Indexes;
using SmsCoordinator;

namespace SmsScheduler
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
            _documentStore = new DocumentStore { Url = "http://localhost:8080", ResourceManagerId = Guid.NewGuid() };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("Configuration");
            IndexCreation.CreateIndexes(typeof(ScheduleMessagesInCoordinatorIndex).Assembly, _documentStore);
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }
}