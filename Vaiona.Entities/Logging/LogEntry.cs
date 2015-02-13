using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vaiona.Entities.Logging
{
    public enum LogType
    {
        Diagnosis,
        Performance,
        Call,
        Exception,
        Data,
        Custom
    }

    public abstract class LogEntry //: EnvironemntLogEntry
    {
        public virtual long Id { get; set; }
        public virtual int VersionNo { get; set; }

        public virtual DateTime UTCDate { get; set; }
        public virtual string CultureId { get; set; }
        public virtual string UserId { get; set; }
        public virtual string RequestURL { get; set; }

        public virtual string Environemt { get; set; }

        public virtual LogType LogType { get; set; }
        public virtual string Desription { get; set; }
        public virtual string ExtraInfo { get; set; }

        public virtual string AssemblyName { get; set; }
        public virtual string AssemblyVersion { get; set; }
        public virtual string ClassName { get; set; }
        public virtual string MethodName { get; set; }
    }

    public class CustomLogEntry : LogEntry
    {
    }
    
    public class MethodLogEntry: LogEntry
    {
        public virtual string Parameters { get; set; }
        public virtual string ParameterValues { get; set; }
        public virtual string ReturnType { get; set; }
        public virtual string ReturnValue { get; set; }

        public virtual long ProcessingTime { get; set; }//in millisecond
    }

    public class DataLogEntry: LogEntry
    {
        public virtual string ObjectId { get; set; } // Id of the data object that the method is working on
        public virtual string ObjectType { get; set; }

        public virtual int GroupId { get; set; }
        public virtual string State { get; set; } // CRUD
    }

    public class RelationLogEntry : LogEntry
    {
        public virtual string SourceObjectId { get; set; } // Id of the data object that the method is working on
        public virtual string SourceObjectType { get; set; }

        public virtual string DestinationObjectId { get; set; }
        public virtual string DestinationObjectType { get; set; }

        public virtual string TransactionId { get; set; }
        public virtual string State { get; set; } // CRUD
    }
}
