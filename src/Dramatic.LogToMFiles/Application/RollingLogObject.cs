// RollingLogObject.cs
// 30-1-2022
// Copyright 2022 Dramatic Development - Victor Vogelpoel
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

namespace Dramatic.LogToMFiles
{
    /// <summary>
    /// Writing Log message to a rolling Log object with LogMessage property.
    /// A log message (part) will be rolled over to another LogObject when either the date changes or the LogMessage property reached its limit (10000 characters).
    /// Remainder of the message will be saved into a new Log object.
    /// </summary>
    internal class RollingLogObject
    {
        private readonly ILogObjectVault  _logVault;
        private readonly string     _logObjectNamePrefix;
        private readonly int        _mfilesLogMessageCharacterLimit = 10000;
        private readonly int        _minCharsForRollover            = 15;

        /// <summary>
        /// Construct the RollingMFilesLogObject with LogVault infrastructure
        /// </summary>
        public RollingLogObject(ILogObjectVault logVault, string logObjectNamePrefix="Log-")
        {
            _logVault               = logVault ?? throw new ArgumentNullException(nameof(logVault));
            _logObjectNamePrefix    = (!String.IsNullOrEmpty(logObjectNamePrefix) ? logObjectNamePrefix : throw new ArgumentNullException(nameof(logObjectNamePrefix)));

            if (logObjectNamePrefix.Length > 50) { throw new ArgumentOutOfRangeException(nameof(logObjectNamePrefix), "logObjectNamePrefix should be a valid string with at most 50 characters"); }
        }

        /// <summary>
        /// Save the log message to a Log object with today's date in its name
        /// </summary>
        /// <param name="logMessage">Log message to save</param>
        public void SaveLogMessage(string logMessage)
        {
            SaveLogMessage(DateTime.Today, logMessage);
        }


        /// <summary>
        /// Save the batchLogMessage into existing or one ore more new Log objects
        /// </summary>
        /// <param name="logDate">Date for the object to save the log message to</param>
        /// <param name="logMessage">Log message to save</param>
        internal void SaveLogMessage(DateTime logDate, string logMessage)
        {
            // If logging structure isn't there, then don't bother going further
            if (!_logVault.IsLogObjectStructurePresent())   { return; }

            // If nothing in the message, then don't bother going further
            if (String.IsNullOrWhiteSpace(logMessage))      { return; }

            // Make sure the batched logMessage has a newline termination
            if (!logMessage.EndsWith(Environment.NewLine))  { logMessage += Environment.NewLine; }

            var logObjectName                   = $"{_logObjectNamePrefix}{logDate:yyyy-MM-dd}";
		    var foundLogObjectsForDate          = _logVault.SearchLogObjects(logObjectName);
		    var foundLogObjectsForDateCount     = foundLogObjectsForDate.Count;
		    var processedBatchedLogMessageIndex = 0;

		    if (foundLogObjectsForDateCount > 0)
		    {
                // Work with the last Log object found
			    var lastLogObjVer               = foundLogObjectsForDate[foundLogObjectsForDateCount-1];

				var currentLogMessage           = _logVault.ReadLogMessageFromLogObject(lastLogObjVer);

                // The M-Files multi-line text property has a limit of 10000 characters. Append lines from the batchedLogMessage up to this limit.
                var charsLeftInCurrentLogMessage= _mfilesLogMessageCharacterLimit - currentLogMessage.Length;

                // Only write the logMessage part to this last log object when there is at least _minCharsForRollover (15) left to write
                // (preventing writing just 15 characters to the current and the remainder to a new Log message?)
                if (charsLeftInCurrentLogMessage > _minCharsForRollover)
                {
                    // Take at most charsLeftInCurrentLogMessage from the batchedLogMessage, but try to get up to the last newline.
				    var logmessageAppendPart    = logMessage.TakeSubstringUpTotheLastNewLine(0, charsLeftInCurrentLogMessage);
				    var appendedLogMessage      = $"{currentLogMessage}{logmessageAppendPart}";

                    var saveSuccess             = _logVault.WriteLogMessageToExistingLogObject(lastLogObjVer, appendedLogMessage);
                    if (saveSuccess)
                    {
				        processedBatchedLogMessageIndex = logmessageAppendPart.Length;
			        }
                }
		    }


            // Now process any leftover batchLogMessage, by creating one or more new Log objects to store remaining parts of the logMessage.
		    while (processedBatchedLogMessageIndex < logMessage.Length)
		    {
			    // Get at most (maxLogObjectMessageSize - batchedLogMessage.length) characters from the logMessage...
			    var logMessagePart = logMessage.TakeSubstringUpTotheLastNewLine(processedBatchedLogMessageIndex, Math.Min(_mfilesLogMessageCharacterLimit, logMessage.Length-processedBatchedLogMessageIndex));

                foundLogObjectsForDateCount++;

                // "Prefix-2021-08-23"
                var newLogObjectName = logObjectName;
                // "Prefix-2021-08-23", "Prefix-2021-08-23 (2)" or "Prefix-2021-08-23 (3)", ....
                if (foundLogObjectsForDateCount > 1) { newLogObjectName += $" ({foundLogObjectsForDateCount})"; }

                // The todaysLogObjectsCount will add "(2)", "(3)", ... to the new Log object title.
			    _logVault.WriteLogMessageToNewLogObject(newLogObjectName, logMessagePart);

			    // Advance the index into the batchedLogMessage until we have written it all.
			    processedBatchedLogMessageIndex += logMessagePart.Length;
		    }
        }
    }
}
