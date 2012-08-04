using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Vaiona.Util.Cfg
{
    // this class must hide the web or desktop config implementation. use VWF pattern
    public class AppConfiguration
    {        
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
                catch { return ("fa-IR"); }
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
                catch { return (string.Empty); }
            }
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
    
    }
}
