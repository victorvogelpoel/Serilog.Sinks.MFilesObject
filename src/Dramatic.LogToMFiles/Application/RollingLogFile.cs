// RollingLogFile.cs
// 25-8-2021
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

namespace Dramatic.LogToMFiles.Application
{
    internal class RollingLogFile
    {
        private readonly ILogFileVault _logVault;
        private readonly string _logObjectNamePrefix;

        /// <summary>
        /// Construct the RollingLogFile.
        /// </summary>
        /// <param name="logVault">vault to search, append or create a Log file object</param>
        /// <param name="logObjectNamePrefix">Prefix to use in the NameOrTitle of the Log document object, default "Log-"</param>
        public RollingLogFile(ILogFileVault logVault, string logObjectNamePrefix="Log-")
        {
            _logVault               = logVault ?? throw new ArgumentNullException(nameof(logVault));
            _logObjectNamePrefix    = (!String.IsNullOrEmpty(logObjectNamePrefix) ? logObjectNamePrefix : throw new ArgumentNullException(nameof(logObjectNamePrefix)));

            if (logObjectNamePrefix.Length > 50) { throw new ArgumentOutOfRangeException(nameof(logObjectNamePrefix), "logObjectNamePrefix should be a valid string with at most 50 characters"); }
        }


        /// <summary>
        /// Save the log message a Log file object with today's date in its name
        /// </summary>
        /// <param name="logMessage">Log message to save</param>
        public void SaveLogMessage(string logMessage)
        {
            SaveLogMessage(DateTime.Today, logMessage);
        }


        /// <summary>
        /// Save the log message a Log file object with the specified date in its name
        /// </summary>
        /// <param name="logDate">Date for the object to save the log message to</param>
        /// <param name="logMessage">Log message to save</param>
        internal void SaveLogMessage(DateTime logDate, string logMessage)
        {
            // If logging structure isn't there, then don't bother going further
            if (!_logVault.IsLogFileStructurePresent())     { return; }

            // If nothing in the message, then don't bother going further
            if (String.IsNullOrWhiteSpace(logMessage))      { return; }

            // Make sure the batched logMessage has a newline termination
            if (!logMessage.EndsWith(Environment.NewLine))  { logMessage += Environment.NewLine; }

            bool createNewLogObject         = true;

            var logObjectName               = $"{_logObjectNamePrefix}{logDate:yyyy-MM-dd}";
            var foundLogFilesForDate        = _logVault.SearchLogFileDocuments(logObjectName);
            var foundLogFilesForDateCount   = foundLogFilesForDate.Count;

            if (foundLogFilesForDateCount > 0)
            {
                var lastLogObjVer = foundLogFilesForDate[foundLogFilesForDateCount-1];

                var saveSuccess = _logVault.WriteLogMessageToExistingLogFile(lastLogObjVer, logMessage);
                createNewLogObject = !saveSuccess;
            }

            if (createNewLogObject)
            {
                foundLogFilesForDateCount++;

                // "Prefix-2021-08-23"
                var newLogFileName = logObjectName;
                // "Prefix-2021-08-23", "Prefix-2021-08-23 (2)" or "Prefix-2021-08-23 (3)", ....
                if (foundLogFilesForDateCount > 1) { newLogFileName += $" ({foundLogFilesForDateCount})"; }

                _logVault.WriteLogMessageToNewLogFile(newLogFileName, logMessage);
            }
        }
    }
}
