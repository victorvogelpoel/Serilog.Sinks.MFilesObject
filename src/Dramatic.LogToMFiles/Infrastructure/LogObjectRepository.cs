// LogObjectRepository.cs
// 24-11-2021
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
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    /// <summary>
    /// Repository for writing to a Log object (objectType 'Log', class 'Log', PropertyDef multi-line text 'Logmessage')
    /// </summary>
    public class LogObjectRepository : ILogMessageRepository
    {
        private readonly RollingLogObject _rollingLogObject;

        /// <summary>
        /// Construct the LogObjectRepository
        /// </summary>
        /// <param name="vault">M-Files vault</param>
        /// <param name="mfilesLogObjectNamePrefix">Prefix for the Log object Name-or-Title. You may want to use the name of the VaultApplication.</param>
        /// <param name="mfilesLogObjectTypeAlias">Alias for the Log ObjectType</param>
        /// <param name="mfilesLogClassAlias">Alias for the Log ClassObject</param>
        /// <param name="mfilesLogMessagePropDefAlias">Alias for the LogMessage PropertyDefinition</param>
        public LogObjectRepository(IVault vault,
                                   string mfilesLogObjectNamePrefix    = DefaultLoggingVaultStructure.LogObjectNamePrefix,
                                   string mfilesLogObjectTypeAlias     = DefaultLoggingVaultStructure.LogObjectTypeAlias,
                                   string mfilesLogClassAlias          = DefaultLoggingVaultStructure.LogClassAlias,
                                   string mfilesLogMessagePropDefAlias = DefaultLoggingVaultStructure.LogMessagePropertyDefinitionAlias)
        {
            if (String.IsNullOrWhiteSpace(mfilesLogObjectNamePrefix))       throw new ArgumentException($"{nameof(mfilesLogObjectNamePrefix)} cannot be null or empty; use something like \"{DefaultLoggingVaultStructure.LogObjectNamePrefix}\".", nameof(mfilesLogObjectNamePrefix));
            if (String.IsNullOrWhiteSpace(mfilesLogObjectTypeAlias))        throw new ArgumentException($"{nameof(mfilesLogObjectTypeAlias)} cannot be null or empty; use something like \"{DefaultLoggingVaultStructure.LogObjectTypeAlias}\"", nameof(mfilesLogObjectTypeAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogClassAlias))             throw new ArgumentException($"{nameof(mfilesLogClassAlias)} cannot be null or empty; use something like \"{DefaultLoggingVaultStructure.LogClassAlias}\"", nameof(mfilesLogClassAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogMessagePropDefAlias))    throw new ArgumentException($"{nameof(mfilesLogMessagePropDefAlias)} cannot be null or empty; use something like \"{DefaultLoggingVaultStructure.LogMessagePropertyDefinitionAlias}\"", nameof(mfilesLogMessagePropDefAlias));

            var logVault        = new LogObjectVault(vault, mfilesLogObjectTypeAlias, mfilesLogClassAlias, mfilesLogMessagePropDefAlias);
            _rollingLogObject   = new RollingLogObject(logVault, mfilesLogObjectNamePrefix);
        }

        /// <summary>
        /// Save the log message to a rolling M-Files Log object
        /// </summary>
        /// <param name="logMessage">message to log to the Log object</param>
        public void SaveLogMessage(string logMessage)
        {
            _rollingLogObject.SaveLogMessage(logMessage);
        }
    }
}
