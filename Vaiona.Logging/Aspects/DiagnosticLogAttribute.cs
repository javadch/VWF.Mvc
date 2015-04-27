using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Aspects;
using Vaiona.Utils.Cfg;
using Vaiona.Entities.Logging;
using System.Diagnostics;
using System.Reflection;

namespace Vaiona.Logging.Aspects
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DiagnoseAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            if (!AppConfiguration.IsLoggingEnable || !AppConfiguration.IsDiagnosticLoggingEnable)
                return;

            var sw = new Stopwatch();
            sw.Start();
            args.MethodExecutionTag = sw;
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            if (!AppConfiguration.IsLoggingEnable || !AppConfiguration.IsDiagnosticLoggingEnable)
                return;
            if (args.Exception != null) // don't log if there is am exception in the call context. it is likely captured by an exception logger
                return;

            var sw = (Stopwatch)args.MethodExecutionTag;
            sw.Stop();

            MethodLogEntry mLog = new MethodLogEntry();

            mLog.UTCDate = AppConfiguration.UTCDateTime;
            mLog.CultureId = AppConfiguration.Culture.Name;
            string tempUser = string.Empty;
            mLog.UserId = Constants.AnonymousUser;
            if (AppConfiguration.TryGetCurrentUser(ref tempUser))
                mLog.UserId = tempUser;
            mLog.RequestURL = AppConfiguration.CurrentRequestURL.ToString();
            mLog.AssemblyName = args.Method.DeclaringType.Assembly.GetName().Name;
            mLog.AssemblyVersion = args.Method.DeclaringType.Assembly.GetName().Version.ToString();
            mLog.ClassName = args.Method.DeclaringType.FullName;
            mLog.MethodName = args.Method.Name.TrimStart("~".ToCharArray());

            String[] strArray;
            try
            {
                ParameterInfo[] pms = args.Method.GetParameters();//.ToList().ForEach(p=> p.ToString())
                strArray = Array.ConvertAll<ParameterInfo, string>(pms, new Converter<ParameterInfo, string>(delegate(ParameterInfo pmi) { return pmi.ToString(); }));
                mLog.Parameters = string.Join(", ", strArray);
            }
            catch (Exception ex) { }
            try
            {
                object[] argArray = args.Arguments.ToArray();
                strArray = Array.ConvertAll<object, string>(argArray,
                    new Converter<object, string>(delegate(object arg)
                        {
                            if (arg != null) return arg.ToString();
                            else return ("NULL");
                        }
                    ));
                mLog.ParameterValues = string.Join(", ", strArray);
            }
            catch (Exception ex) { }
            mLog.ReturnType = args.ReturnValue.GetType().FullName;
            try
            {
                if (args.ReturnValue.ToString().Length <= 255) // just to limit the size to comply with the DB
                {
                    mLog.ReturnValue = args.ReturnValue.ToString();//
                }
                else
                {
                    mLog.ReturnValue = args.ReturnValue.ToString().Substring(0, 252) + "...";
                }
            }
            catch { }
            mLog.ProcessingTime = sw.ElapsedMilliseconds;
            mLog.Desription = "NA";

            mLog.LogType = LogType.Diagnosis;
            LoggerFactory.LogMethod(mLog);
        }
    }
}
