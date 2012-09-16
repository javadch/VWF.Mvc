using System.Web;
using Vaiona.Persistence.Api;
using Vaiona.IoC;
using Vaiona.Util.Cfg;

namespace Vaiona.Web.Context
{
    public static class AppUtils
    {
        public static void Init(HttpApplication app)
        {
            IoCFactory.StartContainer(string.Format("{0}{1}", app.Server.MapPath("~/"), "IoC.config"), "DefaultContainer");
            IPersistenceManager pManager = PersistenceFactory.GetPersistenceManager(); // just to prepare data access environment

            // change this folder path to something relative to the web root
            pManager.Configure(AppConfiguration.DatabaseMappingFile, AppConfiguration.DatabaseDialect, AppConfiguration.DefaultApplicationConnection.ConnectionString);
            if (AppConfiguration.CreateDatabase)
                pManager.ExportSchema();
            pManager.Start();
        }
    }
}