using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vaiona.Persistence.Api
{
    public interface IPersistenceManager
    {
        void Configure(string configFilePath, List<string> mappingFolders, string databaseDilect, string connectionString = "", bool useNeutralMapping = false);
        void ExportSchema(bool generateScript = false, bool executeAgainstTargetDB = true, bool justDrop = false);
        void Start();
        void Shutdown();
        IUnitOfWork CreateUnitOfWork(bool autoCommit = false, bool throwExceptionOnError = true, bool allowMultipleCommit = false
            , EventHandler beforeCommit = null, EventHandler afterCommit = null, EventHandler beforeIgnore = null, EventHandler afterIgnore = null);

        object GetCurrentConversation();
        void StartConversation();
        void EndConversation();
        void EndContext();
    }
}
