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
            IPersistenceManager persistenceManager = IoCFactory.Container.Resolve<IPersistenceManager>() as IPersistenceManager;
            return (persistenceManager);
        }
    }
}
