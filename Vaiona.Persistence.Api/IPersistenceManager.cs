using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vaiona.Util.Cfg;

namespace Vaiona.Persistence.Api
{
    public interface IPersistenceManager
    {
        object Factory { get; }
        void Configure(string connectionString = "", string databaseDilect = "DB2Dialect", string fallbackFoler = "Default", bool showQueries = false);
        void ExportSchema(bool generateScript = false, bool executeAgainstTargetDB = true, bool justDrop = false);
        void UpdateSchema(bool generateScript = false, bool executeAgainstTargetDB = true);
        void Start();
        void Shutdown();
        IUnitOfWork CreateUnitOfWork(bool autoCommit = false, bool throwExceptionOnError = true, bool allowMultipleCommit = false
            , EventHandler beforeCommit = null, EventHandler afterCommit = null, EventHandler beforeIgnore = null, EventHandler afterIgnore = null);

        object GetCurrentConversation();
        void StartConversation();
        /// <summary>
        /// Closes the session and rollbacks all the changes
        /// </summary>
        void ShutdownConversation();

        /// <summary>
        /// Closes the session but first commits all the changes
        /// </summary>
        void EndConversation();
        void EndContext();
    }
}
