using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Web;
using System.Globalization;
using System.Threading;
using System.Security.Principal;
using System.Web.Security;

namespace Vaiona.Utils.Cfg
{
    public class Constants
    {
        public static readonly string AnonymousUser = @"Anonymous";
        public static readonly string EveryoneRole = "Everyone";
    }

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

        //public static string GetLoggerInfo(string logType)
        //{
        //    string loggerKey = logType + ".Logger";
        //    string loggerInfo = ""; // the logger info is the FQN of the logger class, that is instantiated by the logger factory
        //    try
        //    {
        //        loggerInfo = ConfigurationManager.AppSettings[loggerKey];
        //    }
        //    catch { loggerInfo = ""; }
        //    if (string.IsNullOrWhiteSpace(loggerInfo)) // try the general logging info attached to the General.Logger
        //    {
        //        loggerKey = "General.Logger";
        //        try
        //        {
        //            loggerInfo = ConfigurationManager.AppSettings[loggerKey];
        //        }
        //        catch { loggerInfo = ""; }
        //    }
        //    return loggerInfo;
        //}

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
                for (int i = 0; i <= level; i++)
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

        public static bool ShowQueries
        {
            get
            {
                try
                {
                    return (bool.Parse(ConfigurationManager.AppSettings["ShowQueries"]));
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

        public static HttpContext HttpContext
        {
            get { return HttpContext.Current; }
        }

        public static Thread CurrentThread
        {
            get { return Thread.CurrentThread; }
        }

        public static CultureInfo UICulture // it can be done in cooperation with session management class
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
        }

        public static CultureInfo Culture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
        }

        public static DateTime UTCDateTime
        {
            get { return (DateTime.UtcNow); }
        }

        public static byte[] GetUTCAsBytes()
        {
            return (System.Text.Encoding.UTF8.GetBytes(AppConfiguration.UTCDateTime.ToBinary().ToString()));
        }

        public static DateTime DateTime
        {
            get { return (DateTime.Now); }
        }

        public static Uri CurrentRequestURL
        {
            get
            {
                try
                {
                    return (HttpContext.Current.Request.Url);
                }
                catch
                {
                    return new Uri("NotFound.htm");
                }
            }
        }

        public static IPrincipal User
        {
            // decide wether user must be authenticated or not
            get
            {
                if (/*Environment.HttpContext.User.Identity != null &&*/ !AppConfiguration.HttpContext.User.Identity.IsAuthenticated)
                {
                    return (createUser(Constants.AnonymousUser, Constants.EveryoneRole));
                }
                else
                    return (AppConfiguration.HttpContext.User);
            }
        }

        public static bool TryGetCurrentUser(ref string userName)
        {
            userName = AppConfiguration.HttpContext.User.Identity.Name; // Thread.CurrentPrincipal.Identity.Name; 
            return (Thread.CurrentPrincipal.Identity.IsAuthenticated); //Thread.CurrentPrincipal.Identity.IsAuthenticated
        }

        internal static IPrincipal createUser(string userName, string roleName)
        {
            IIdentity identity = new GenericIdentity(userName);
            RolePrincipal p = new RolePrincipal(identity);
            //string[] roles = new string[1]; // get it from the role provder, take into account site and portal id
            //roles[0] = string.IsNullOrEmpty(roleName) ? Constants.GuestRole : roleName;
            //return (new GenericPrincipal(identity, roles));
            return (p);
            //JApplication.CurrentHttpContext.User.Identity.IsAuthenticated = 

        }

        public static bool IsLoggingEnable
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["IsLoggingEnable"].ToString()); }
        }

        public static bool IsPerformanceLoggingEnable
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["IsPerformanceLoggingEnable"].ToString()); }
        }

        public static bool IsDiagnosticLoggingEnable
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["IsDiagnosticLoggingEnable"].ToString()); }
        }

        public static bool IsCallLoggingEnable
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["IsCallLoggingEnable"].ToString()); }
        }

        public static bool IsExceptionLoggingEnable
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["IsExceptionLoggingEnable"].ToString()); }
        }

        public static bool IsDataLoggingEnable
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["IsDataLoggingEnable"].ToString()); }
        }

        //public static bool IsCustomLoggingEnable
        //{
        //    get { return bool.Parse(ConfigurationManager.AppSettings["IsCustomLoggingEnable"].ToString()); }
        //}
    }
}
