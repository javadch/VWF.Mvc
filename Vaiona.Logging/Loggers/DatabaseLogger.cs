using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vaiona.Persistence.Api;
using Vaiona.Entities.Logging;

namespace Vaiona.Logging.Loggers
{
    public class DatabaseLogger : ILogger
    {
        public void LogMethod(MethodLogEntry logEntry)
        {
            IPersistenceManager pManager = PersistenceFactory.GetPersistenceManager();
            using (IUnitOfWork unit = pManager.CreateIsolatedUnitOfWork(false, true, false, null, null, null, null))
            {
                IRepository<MethodLogEntry> repo = unit.GetRepository<MethodLogEntry>();
                repo.Put(logEntry);
                unit.Commit();
            }
        }

        public void LogData(Entities.Logging.DataLogEntry logEntry)
        {
            throw new NotImplementedException();
        }

        public void LogRelation(Entities.Logging.RelationLogEntry logEntry)
        {
            throw new NotImplementedException();
        }

        public void LogCustom(CustomLogEntry logEntry)
        {
            IPersistenceManager pManager = PersistenceFactory.GetPersistenceManager();
            using (IUnitOfWork unit = pManager.CreateIsolatedUnitOfWork(false, true, false, null, null, null, null))
            {
                IRepository<CustomLogEntry> repo = unit.GetRepository<CustomLogEntry>();
                repo.Put(logEntry);
                unit.Commit();
            }
        }
    }
}
