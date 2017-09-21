using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaiona.Entities.Logging;
using Vaiona.Utils.Cfg;

namespace Vaiona.Logging.Loggers
{
    public class FileLogger : ILogger
    {
        //private static FileLogger logger = new FileLogger();
        private FileLogger()
        {
        }

        public static FileLogger GetInstance()
        {
            return new FileLogger();// logger;
        }

        private string buildLogFileName()
        {
            string serialNo = string.Format("{0}.{1}.{2}", DateTime.UtcNow.Day, DateTime.UtcNow.Month, DateTime.UtcNow.Year);
            string fileName = "bexis." + serialNo + ".log";
            string logFolder = Path.Combine(AppConfiguration.WorkspaceGeneralRoot, "Logging");
            if(!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }
            string logFile = Path.Combine(logFolder, fileName);
            return logFile;
        }
        public void LogCustom(CustomLogEntry logEntry)
        {
            throw new NotImplementedException();
        }

        // This mechanism must be replaced with a robust solution. It is only experimental.
        public void LogCustom(string message)
        {
            FileStream stream = null;
            StreamWriter streamWriter = null;
            try
            {
                string logFile = buildLogFileName();
                stream = new FileStream(logFile, FileMode.Append, FileAccess.Write);
                streamWriter = new StreamWriter((Stream)stream);
                string wrappedMessage = string.Format("{0}: {1}", DateTime.UtcNow, message);
                streamWriter.WriteLine(wrappedMessage);
                
            }
            catch { }
            finally
            {
                try
                {
                    stream.Close();
                    streamWriter.Close();
                }
                catch { }
            }
        }

        public void LogData(DataLogEntry logEntry)
        {
            throw new NotImplementedException();
        }

        public void LogMethod(MethodLogEntry logEntry)
        {
            throw new NotImplementedException();
        }

        public void LogRelation(RelationLogEntry logEntry)
        {
            throw new NotImplementedException();
        }
    }
}
