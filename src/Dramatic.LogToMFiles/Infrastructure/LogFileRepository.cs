// LogFileRepository.cs
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
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    /// <summary>
    /// Repository for writing to a Log File object (objectType 'Document', class 'Log')
    /// </summary>
    public class LogFileRepository : ILogMessageRepository
    {
        private readonly RollingLogFile _rollingFile;

        /// <summary>
        /// Construct the LogFileRepository
        /// </summary>
        /// <param name="vault">M-Files vault</param>
        /// <param name="mfilesLogObjectNamePrefix">Prefix for the Log object Name-or-Title. You may want to use the name of the VaultApplication.</param>
        /// <param name="mfilesLogFileClassAlias">Alias for the Log file class</param>
        public LogFileRepository( IVault vault,
                                        string mfilesLogFileNamePrefix  = DefaultLoggingVaultStructure.LogObjectNamePrefix,
                                        string mfilesLogFileClassAlias  = DefaultLoggingVaultStructure.LogFileClassAlias)
        {
            if (String.IsNullOrWhiteSpace(mfilesLogFileNamePrefix)) throw new ArgumentException($"{nameof(mfilesLogFileNamePrefix)} cannot be null or empty; use something like \"{DefaultLoggingVaultStructure.LogObjectNamePrefix}\".", nameof(mfilesLogFileNamePrefix));
            if (String.IsNullOrWhiteSpace(mfilesLogFileClassAlias)) throw new ArgumentException($"{nameof(mfilesLogFileClassAlias)} cannot be null or empty; use something like \"{DefaultLoggingVaultStructure.LogFileClassAlias}\"", nameof(mfilesLogFileClassAlias));

            var logVault = new LogFileVault(vault, mfilesLogFileClassAlias);
            _rollingFile = new RollingLogFile(logVault, mfilesLogFileNamePrefix);
        }

        /// <summary>
        /// Save the log message to a rolling M-Files Log File
        /// </summary>
        /// <param name="logMessage">message to log to the Log object</param>
        public void SaveLogMessage(string logMessage)
        {
            _rollingFile.SaveLogMessage(logMessage);
        }

    }
}
