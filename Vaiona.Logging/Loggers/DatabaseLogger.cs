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
        IPersistenceManager pManager = PersistenceFactory.GetPersistenceManager();
        public void LogMethod(MethodLogEntry logEntry)
        {
            using (IUnitOfWork unit = pManager.CreateIsolatedUnitOfWork(false, true, false, null, null, null, null))
            {
                IRepository<MethodLogEntry> repo = unit.GetRepository<MethodLogEntry>();
                repo.Put(logEntry);
                unit.Commit();
            }
        }

        public void LogData(Entities.Logging.DataLogEntry logEntry)
        {
            using (IUnitOfWork unit = pManager.CreateIsolatedUnitOfWork(false, true, false, null, null, null, null))
            {
                IRepository<DataLogEntry> repo = unit.GetRepository<DataLogEntry>();
                repo.Put(logEntry);
                unit.Commit();
            }
        }

        public void LogRelation(Entities.Logging.RelationLogEntry logEntry)
        {
            using (IUnitOfWork unit = pManager.CreateIsolatedUnitOfWork(false, true, false, null, null, null, null))
            {
                IRepository<RelationLogEntry> repo = unit.GetRepository<RelationLogEntry>();
                repo.Put(logEntry);
                unit.Commit();
            }
        }

        public void LogCustom(CustomLogEntry logEntry)
        {
            using (IUnitOfWork unit = pManager.CreateIsolatedUnitOfWork(false, true, false, null, null, null, null))
            {
                IRepository<CustomLogEntry> repo = unit.GetRepository<CustomLogEntry>();
                repo.Put(logEntry);
                unit.Commit();
            }
        }
    }
}
