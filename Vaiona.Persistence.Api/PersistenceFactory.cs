using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vaiona.IoC;

namespace Vaiona.Persistence.Api
{
    public static class PersistenceFactory
    {
        public static IPersistenceManager GetPersistenceManager()
        {
            IPersistenceManager persistenceManager = null;
            try
            {
                persistenceManager = IoCFactory.Container.Resolve<IPersistenceManager>() as IPersistenceManager;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not load persistence manager", ex);
            }
            return (persistenceManager);
        }
    }
}
