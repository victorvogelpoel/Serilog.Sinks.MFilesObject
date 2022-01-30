// LogVaultStub.cs
// 3-9-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections.Generic;
using System.Linq;
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
