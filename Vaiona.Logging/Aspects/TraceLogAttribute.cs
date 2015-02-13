using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Aspects;
using Vaiona.Utils.Cfg;
using Vaiona.Entities.Logging;

namespace Vaiona.Logging.Aspects
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RecordCallAttribute : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {
            if (!AppConfiguration.IsLoggingEnable || !AppConfiguration.IsTraceLoggingEnable)
                return;
            base.OnInvoke(args);
            /// if invocation of the original method encounters any exception, 
            /// the remaining code will not execute, so there is no need to 
            /// guard this logging from Exceptions, in contrast to performance or diagnostic loggings

            MethodLogEntry mLog = new MethodLogEntry();

            mLog.UTCDate = AppConfiguration.UTCDateTime;
            mLog.CultureId = AppConfiguration.Culture.Name;
            string tempUser = string.Empty;
            mLog.UserId = Constants.AnonymousUser;
            if (AppConfiguration.TryGetCurrentUser(ref tempUser))
                mLog.UserId = tempUser;
            mLog.RequestURL = AppConfiguration.CurrentRequestURL.ToString();
            //mLog = (MethodLogEntry)LoggerFactory.GetEnvironemntLogEntry();

            mLog.AssemblyName = args.Method.DeclaringType.Assembly.GetName().Name;//
            mLog.AssemblyVersion = args.Method.DeclaringType.Assembly.GetName().Version.ToString();
            mLog.ClassName = args.Method.DeclaringType.FullName;
            mLog.MethodName = args.Method.Name.TrimStart("~".ToCharArray()); // PostSharp renames the original method name be adding a leading tilde "~"

            mLog.LogType = LogType.Call;
            LoggerFactory.LogMethod(mLog);
        }
    }
}
