using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Web;

namespace Vaiona.Util.Cfg
{
    // this class must hide the web or desktop config implementation. use VWF pattern
    public class AppConfiguration
    {
        public static bool IsWebContext
        {
            get
            {
                return HttpContext.Current != null;
            }
        }

        public static ConnectionStringSettings DefaultApplicationConnection
        {
            get
            {
                return (ConfigurationManager.ConnectionStrings["ApplicationServices"]);
            }
        }

        public static string DefaultCulture
        {
            get
            {
                try
                {
                    return (ConfigurationManager.AppSettings["DefaultCulture"]);
                }
                catch { return ("en-US"); }
            }
        }

        public static string DatabaseMappingFile
        {
            get
            {
                try
                {
                    return (ConfigurationManager.AppSettings["DatabaseMappingFile"]);
                }
                catch { return (string.Empty); }
            }
        }

        public static string DatabaseDialect
        {
            get
            {
                try
                {
                    return (ConfigurationManager.AppSettings["DatabaseDialect"]);
                }
                catch { return ("DB2Dialect"); }
            }
        }

        public static bool AutoCommitTransactions
        {
            get
            {
                try
                {
                    return (bool.Parse(ConfigurationManager.AppSettings["AutoCommitTransactions"]));
                }
                catch { return (false); }
            }
        }

        public static string IoCProviderTypeInfo
        {
            get
            {
                try
                {
                    return (ConfigurationManager.AppSettings["IoCProviderTypeInfo"]);
                }
                catch { return (string.Empty); }
            }
        }

        public static string AppRoot
        {
            get
            {
                return (AppDomain.CurrentDomain.BaseDirectory);
            }
        }

        /// <summary>
        /// DataPath shows the root folder containing business data. 
        /// Its should be defined in the web.config otherwise it returns the application root folder
        /// </summary>
        public static string DataPath
        {
            get
            {
                string path = AppRoot;
                try
                {
                    path = ConfigurationManager.AppSettings["DataPath"];
                }
                catch{}
                path = (string.IsNullOrWhiteSpace(path)? AppRoot: path);
                return (path);
            }
        }

        private static string workspaceRootPath = string.Empty;
        public static string WorkspaceRootPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(workspaceRootPath))
                    return (workspaceRootPath);
                string path = string.Empty;
                try
                {
                    path = ConfigurationManager.AppSettings["WorkspacePath"];
                }
                catch { path = string.Empty; }
                int level = 0;
                if (string.IsNullOrWhiteSpace(path)) // its a relative path at the same level with the web.config
                    level = 0;
                else if (path.Contains(@"..\")) // its a relative path but upper than web.config. the number of ..\ patterns shows how many level upper
                {
                    level = path.Split(@"\".ToCharArray()).Length - 1;
                }
                else // its an absolute path, just return it
                {
                    workspaceRootPath = path;
                    return (workspaceRootPath);
                }
                // use a default location: the same level with the app root not beneath it
                DirectoryInfo di = new DirectoryInfo(AppRoot);
                while (di.GetFiles("Web.config").Count() >= 1)
                    di = di.Parent;
                for (int i = 0; i < level; i++)
                {
                    di = di.Parent;
                }
                workspaceRootPath = Path.Combine(di.FullName, "Workspace");
                return (workspaceRootPath);
            }
        }

        public static string WorkspaceComponentRoot
        {
            get
            {
                try
                {
                    return (Path.Combine(WorkspaceRootPath, "Components"));
                }
                catch { return (string.Empty); }
            }
        }

        public static string WorkspaceModulesRoot
        {
            get
            {
                try
                {
                    return (Path.Combine(WorkspaceRootPath, "Modules"));
                }
                catch { return (string.Empty); }
            }
        }

        public static string WorkspaceGeneralRoot
        {
            get
            {
                try
                {
                    return (Path.Combine(WorkspaceRootPath, "General"));
                }
                catch { return (string.Empty); }
            }
        }

        public static string GetModuleWorkspacePath(string moduleName)
        {
            return (Path.Combine(WorkspaceModulesRoot, moduleName));
        }

        public static string GetComponentWorkspacePath(string componentName)
        {
            return (Path.Combine(WorkspaceComponentRoot, componentName));
        }

        public static bool UseSchemaInDatabaseGeneration
        {
            get
            {
                try
                {
                    return (bool.Parse(ConfigurationManager.AppSettings["UseSchemaInDatabaseGeneration"]));
                }
                catch { return (false); }
            }
        }

        public static bool CreateDatabase
        {
            get
            {
                try
                {
                    return (bool.Parse(ConfigurationManager.AppSettings["CreateDatabase"]));
                }
                catch { return (false); }
            }
        }

        public static string ThemesPath
        {
            get
            {
                try
                {
                    return (ConfigurationManager.AppSettings["ThemesPath"]);
                }
                catch { return ("~/Themes"); }
            }
        }

        public static string DefaultThemeName
        {
            get
            {
                try
                {
                    return (ConfigurationManager.AppSettings["DefaultThemeName"]);
                }
                catch { return ("Default"); }
            }
        }

        public static string ActiveLayoutName
        {
            get
            {
                try
                {
                    return (ConfigurationManager.AppSettings["ActiveLayoutName"]);
                }
                catch { return ("_Layout"); }
            }
        }

        public static bool ThrowErrorWhenParialContentNotFound
        {
            get
            {
                try
                {
                    return (bool.Parse(ConfigurationManager.AppSettings["ThrowErrorWhenParialContentNotFound"]));
                }
                catch { return (false); }
            }
        }
    }
}
