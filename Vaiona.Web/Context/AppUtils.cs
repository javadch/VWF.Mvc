using System.Web;
using Vaiona.Persistence.Api;
using Vaiona.IoC;
using Vaiona.Util.Cfg;
using System.Linq;
using System.Collections.Generic;

namespace Vaiona.Web.Context
{
    public static class AppUtils
    {
        public static void Init(HttpApplication app)
        {
            IoCFactory.StartContainer(string.Format("{0}{1}", app.Server.MapPath("~/"), "IoC.config"), "DefaultContainer");
            IPersistenceManager pManager = PersistenceFactory.GetPersistenceManager(); // just to prepare data access environment

            // change this folder path to something relative to the web root
            //Its possible to have more than one mapping folder, but the first one should point to the place of configuration file too.
            System.Collections.Generic.List<string> mappingFolders  = AppConfiguration.DatabaseMappingFile.Split(',').ToList();
            pManager.Configure(mappingFolders, AppConfiguration.DatabaseDialect, AppConfiguration.DefaultApplicationConnection.ConnectionString);
            if (AppConfiguration.CreateDatabase)
                pManager.ExportSchema();
            pManager.Start();
        }
    }
}