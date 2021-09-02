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

        public List<ObjVer> SearchLogObjects(string logObjectBaseName)
        {
            return LogObjects.Where(o => o.NameOrTitle.StartsWith(logObjectBaseName, StringComparison.Ordinal)).Select(p=>p.ObjVer).ToList();
        }

        public bool WriteLogMessageToExistingLogObject(ObjVer logObjVer, string logMessage)
        {
            var logObject           = LogObjects[logObjVer.ID - 1];
            logObject.LogMessage   = logMessage;

            return true;
        }

        public bool WriteLogMessageToNewLogObject(string newLogObjectName, string logMessage)
        {
            var logObjectStub = new LogObjectStub
            {
                NameOrTitle = newLogObjectName,
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
