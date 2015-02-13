using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vaiona.Entities.Logging;
using Vaiona.Utils.Cfg;
using System.Diagnostics;

namespace Vaiona.Logging
{
    public class LoggerFactory
    {
        /// <summary>
        /// based on configuration info and provided logType, choose one of the concrete loggers, 
        /// search for the specific logType (e.g., Performance.Logging), then for General (General.Logging), and then for the no named registration in the IoC            
        /// object creation and lifetime is managed by the IoC container. No need to keep a singleton or static object reference here.
        /// </summary>
        /// <param name="LogType"></param>
        /// <returns></returns>
        private static ILogger create(LogType LogType)
        {
            string loggerKey = string.Format("{0}.Logging", LogType);
            if(IoC.IoCFactory.Container.IsRegistered<ILogger>(loggerKey))
                return IoC.IoCFactory.Container.Resolve<ILogger>(loggerKey);
            loggerKey = "General.Logging";
            if (IoC.IoCFactory.Container.IsRegistered<ILogger>(loggerKey))
                return IoC.IoCFactory.Container.Resolve<ILogger>(loggerKey);
            return IoC.IoCFactory.Container.Resolve<ILogger>();            
        }

        private static void refineLogEntry(LogEntry logEntry)
        {
            logEntry.Environemt = string.Join(", ", logEntry.Environemt, string.Format("Server OS={0}, Server .NET={1}", Environment.OSVersion, Environment.Version));
            if (AppConfiguration.HttpContext != null && AppConfiguration.HttpContext.Request != null)
            {
                logEntry.Environemt = string.Join(", ",  logEntry.Environemt, string.Format("UserAgent={0}, HttpMethod={1}, IsSecureConnection={2}, UserHostAddress={3} ({4})",
                                                                            AppConfiguration.HttpContext.Request.UserAgent,
                                                                            AppConfiguration.HttpContext.Request.HttpMethod,
                                                                            AppConfiguration.HttpContext.Request.IsSecureConnection,
                                                                            AppConfiguration.HttpContext.Request.UserHostAddress,
                                                                            (AppConfiguration.HttpContext.Request.IsLocal? "Local Request": "Remote Request")
                                                                            )
                                                 );
            }
            logEntry.Environemt = logEntry.Environemt.TrimStart(", ".ToCharArray());

            logEntry.ExtraInfo = string.Join(", ", logEntry.ExtraInfo, string.Format("SessionID={0}", AppConfiguration.HttpContext.Session.SessionID))
                                       .TrimStart(", ".ToCharArray());
        }

        delegate void CustomLogDelegate(CustomLogEntry logEntry);
        public static void LogCustom(CustomLogEntry logEntry)
        {
            if (!AppConfiguration.IsLoggingEnable || !AppConfiguration.IsCustomLoggingEnable)
                return;
            ILogger logger = create(logEntry.LogType);
            refineLogEntry(logEntry);
            CustomLogDelegate dlgt = new CustomLogDelegate(logger.LogCustom);
            IAsyncResult ar = dlgt.BeginInvoke(logEntry, null, null);
        }

        public static void LogCustom(string message)
        {
            if (!AppConfiguration.IsLoggingEnable || !AppConfiguration.IsCustomLoggingEnable)
                return;
            CustomLogEntry logEntry = new CustomLogEntry();
            logEntry.LogType = LogType.Custom;
            logEntry.Desription = message;
            logEntry.UTCDate = AppConfiguration.UTCDateTime;
            logEntry.CultureId = AppConfiguration.Culture.Name;
            string tempUser = string.Empty;
            logEntry.UserId = Constants.AnonymousUser;
            if (AppConfiguration.TryGetCurrentUser(ref tempUser))
                logEntry.UserId = tempUser;
            logEntry.RequestURL = AppConfiguration.CurrentRequestURL.ToString();

            // caller method information. indicates where the custom logging function were called
            StackFrame frame = new StackFrame(1);
            logEntry.AssemblyName = frame.GetMethod().DeclaringType.Assembly.GetName().Name;//
            logEntry.AssemblyVersion = frame.GetMethod().DeclaringType.Assembly.GetName().Version.ToString();
            logEntry.ClassName = frame.GetMethod().DeclaringType.FullName;
            logEntry.MethodName = frame.GetMethod().Name.TrimStart("<".ToCharArray()).TrimEnd(">".ToCharArray()); // not obvious why the method name is wrapped in <>??
            LogCustom(logEntry);
        }

        delegate void MethodLogDelegate(MethodLogEntry logEntry);
        public static void LogMethod(MethodLogEntry logEntry)
        {
            ILogger logger = create(logEntry.LogType);
            refineLogEntry(logEntry);
            MethodLogDelegate dlgt = new MethodLogDelegate(logger.LogMethod);
            IAsyncResult ar = dlgt.BeginInvoke(logEntry, null, null);
        }

        delegate void DataLogDelegate(DataLogEntry logEntry);
        public static void LogData(DataLogEntry logEntry) // subject to remove
        {
            ILogger logger = create(logEntry.LogType);
            refineLogEntry(logEntry);
            DataLogDelegate dlgt = new DataLogDelegate(logger.LogData);
            IAsyncResult ar = dlgt.BeginInvoke(logEntry, null, null);
        }

        //public static void LogData(DataLogEntry logEntry)
        //{
        //    ILogger logger = create(logEntry.LogType);
        //    refineLogEntry(logEntry);
        //    logger.LogData(logEntry);
        //}

        public static void LogDataRelation(RelationLogEntry logEntry)
        {
            ILogger logger = create(logEntry.LogType);
            refineLogEntry(logEntry);
            logger.LogRelation(logEntry);
        }
    }
}
