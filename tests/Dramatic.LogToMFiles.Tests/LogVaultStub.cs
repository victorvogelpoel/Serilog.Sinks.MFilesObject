using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dramatic.LogToMFiles.Application;
using MFilesAPI;

namespace Dramatic.LogToMFiles.Tests
{
    internal class LogObjectStub
    {
        public string NameOrTitle { get; set; }
        public string LogMessage { get; set; }
        public ObjVer ObjVer { get; set; }
    }


    internal class LogVaultStub : ILogObjectVault
    {
        public List<LogObjectStub> LogObjects { get; } = new List<LogObjectStub>();

        public LogVaultStub()
        {
        }

        public bool IsLogObjectStructurePresent()
        {
            return true;
        }

        public string ReadLogMessageFromLogObject(ObjVer logObjVer)
        {
            return LogObjects[logObjVer.ID - 1].LogMessage;
        }

        public List<ObjVer> SearchLogObjects(string logObjectNamePrefix, DateTime logDate)
        {
            var targetNameOrTitle = $"{logObjectNamePrefix}{logDate:yyyy-MM-dd}";

            return LogObjects.Where(o => o.NameOrTitle.StartsWith(targetNameOrTitle, StringComparison.Ordinal)).Select(p=>p.ObjVer).ToList();
        }

        public bool WriteLogMessageToExistingLogObject(ObjVer logObjVer, string logMessage)
        {
            var logObject           = LogObjects[logObjVer.ID - 1];
            logObject.LogMessage   = logMessage;

            return true;
        }

        public bool WriteLogMessageToNewLogObject(string logObjectNamePrefix, DateTime logDate, int logObjectOrdinal, string logMessage)
        {
            var logObjectTitle = $"{logObjectNamePrefix}{logDate:yyyy-MM-dd}";
            // "Prefix-2021-08-23 (2)", "Prefix-2021-08-23 (3)", ....
            if (logObjectOrdinal > 1) { logObjectTitle += $" ({logObjectOrdinal})"; }

            var logObjectStub = new LogObjectStub
            {
                NameOrTitle = logObjectTitle,
                LogMessage  = logMessage,
                ObjVer      = new ObjVerClass
                {
                    ID = LogObjects.Count + 1
                }
            };

            LogObjects.Add(logObjectStub);

            return true;
        }
    }
}
