// LogFileVault.cs
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    public class LogFileVault : ILogFileVault
    {
        private readonly IVault _vault;
        private readonly string _mfilesLogFileClassAlias;
        private readonly Random _rnd = new Random();
        private int _mfilesLogFileClassID;

        public LogFileVault(IVault vault, string mfilesLogFileClassAlias  = DefaultLoggingVaultStructure.LogFileClassAlias)
        {
            if (String.IsNullOrWhiteSpace(mfilesLogFileClassAlias)) throw new ArgumentException($"{nameof(mfilesLogFileClassAlias)} cannot be null or empty; use something like \"PD.Serilog.MFilesObjectLogSink.LogFile\"", nameof(mfilesLogFileClassAlias));

            _vault                      = vault ?? throw new ArgumentNullException(nameof(vault));
            _mfilesLogFileClassAlias    = mfilesLogFileClassAlias;
        }


        private void ReadLoggingStructureIDs()
        {
            // Get the vault structure IDs for the aliases.
            // An operation call will return -1 when the alias cannot be found.

            _mfilesLogFileClassID       = _vault.ClassOperations.GetObjectClassIDByAlias(_mfilesLogFileClassAlias);

        }

        public bool IsLogFileStructurePresent()
        {
            ReadLoggingStructureIDs();

            return (_mfilesLogFileClassID != -1);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="logFileBaseName"></param>
        /// <returns></returns>
        public List<ObjVer> SearchLogFileDocuments(string logFileBaseName)
        {
            var excludeDeletedItemSearchCondition = new SearchCondition();
            excludeDeletedItemSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);
            excludeDeletedItemSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            excludeDeletedItemSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);

            // Search for class LogFile
            var logFileClassSearchCondition = new SearchCondition();
            logFileClassSearchCondition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass, MFParentChildBehavior.MFParentChildBehaviorNone);
            logFileClassSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            logFileClassSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogFileClassID);

            var titleDefSearchCondition = new SearchCondition();
            titleDefSearchCondition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle, MFParentChildBehavior.MFParentChildBehaviorNone);

            // We want only LogFile class objects which NameOrTitle begins with the search string provided.
            titleDefSearchCondition.ConditionType = MFConditionType.MFConditionTypeStartsWith;

            titleDefSearchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, logFileBaseName);  // eg "Log-2021-05-12"

            var searchConditions    = new SearchConditions
            {
                { -1, excludeDeletedItemSearchCondition },
                { -1, logFileClassSearchCondition },
                { -1, titleDefSearchCondition }
            };

            var searchResults       = _vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions, MFSearchFlags.MFSearchFlagNone, SortResults: false);

            // return 0, 1 or more Log objects for the current date, like "Log-2021-05-12", "Log-2021-05-12 (2)". They may not be sorted on title; we'll sort later on CreatedUtc

            if (searchResults.Count > 0 )
            {
                // Found one or more Log objects for today. First sort it on CreatedUtc
                searchResults.Sort(new LogObjectCreatedComparer());
            }

            var todaysLogObjects = new List<ObjVer>();
            for (int i=1; i<=searchResults.Count; i++) { todaysLogObjects.Add(searchResults[i].ObjVer); }

            return todaysLogObjects;

        }

        /// <summary>
        ///  Verify if the specified logObjVer is checked in. If checked out, then try 4 more times to see if the object gets checked in, with some random time between.
        /// </summary>
        /// <param name="logObjVer">Log object version to verify for check-in status</param>
        /// <returns>true if the Log object version is checked in, false if it is checked out</returns>
        private bool IsObjectCheckedIn(ObjVer logObjVer)
        {
            bool logObjectIsCheckedOut;
            var maxTestRetries = 5;

            do
            {
                maxTestRetries--;

                logObjectIsCheckedOut = _vault.ObjectOperations.IsObjectCheckedOut(logObjVer.ObjID);
                if (maxTestRetries > 0 && logObjectIsCheckedOut) { Thread.Sleep(millisecondsTimeout: _rnd.Next(200, 1000)); }

            } while (maxTestRetries > 0 && logObjectIsCheckedOut);

            return !logObjectIsCheckedOut;
        }


        public bool WriteLogMessageToExistingLogFile(ObjVer logObjVer, string logMessage)
        {
            ObjectVersion checkedOutLogFileObjectVersion = null;

            try
            {
                if (IsObjectCheckedIn(logObjVer))
                {
                    // And get its file(s)
                    var currentLogFiles = _vault.ObjectFileOperations.GetFiles(logObjVer);
                    if (currentLogFiles.Count == 1)
                    {
                        var currentLogFile = currentLogFiles[1];

                        // Check out the LogFile object
                        checkedOutLogFileObjectVersion  = _vault.ObjectOperations.CheckOut(logObjVer.ObjID);

                        // And get the future log file version
                        var checkedOutLogFiles          = _vault.ObjectFileOperations.GetFiles(checkedOutLogFileObjectVersion.ObjVer);
                        var checkedOutLogFile           = checkedOutLogFiles[1];

                        // Download file, add log message and upload again
                        string logFileTempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                        try
                        {
                            _vault.ObjectFileOperations.DownloadFile(currentLogFile.ID, currentLogFile.Version, logFileTempFilePath);

                            File.AppendAllText(logFileTempFilePath, logMessage, Encoding.UTF8);

                            _vault.ObjectFileOperations.UploadFile(checkedOutLogFile.ID, checkedOutLogFile.Version, logFileTempFilePath);

                            // And at last, check in the existing Log object again.
                            _vault.ObjectOperations.CheckIn(checkedOutLogFileObjectVersion.ObjVer);
                        }
                        finally
                        {
                            if (!String.IsNullOrEmpty(logFileTempFilePath) && File.Exists(logFileTempFilePath))
                            {
                                File.Delete(logFileTempFilePath);
                            }
                        }

                        return true;
                    }
                }
                // else: the object is checked out even after retries, so we're going to create a new Log object for the batchedLogMessage
            }
            catch
            {
                // Any errors updating an existing Log object, then undo the checkout, and we'll try and create a new Log object for the batchedLogMessage instead
                if (null != checkedOutLogFileObjectVersion)
                {
                    _vault.ObjectOperations.UndoCheckout(checkedOutLogFileObjectVersion.ObjVer);
                }
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="logObjectNamePrefix"></param>
        /// <param name="logDate"></param>
        /// <param name="logObjectOrdinal"></param>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        public bool WriteLogMessageToNewLogFile(string newLogFileName, string logMessage)
        {
            string logFileTempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                // Write the logMessage to the temp file
                File.WriteAllText(logFileTempFilePath, logMessage, Encoding.UTF8);

                // Class "LogFile"
                var classPV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass };
                classPV.Value.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogFileClassID);  // "LogFile" class

                // Prop "NameOrTitle"
                var titlePV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle };
                titlePV.Value.SetValue(MFDataType.MFDatatypeText, newLogFileName);     // eg "Log-2021-05-26", or "Log-2021-05-26 (2)"

                var propertyValues = new PropertyValues
                {
                    { -1, classPV },
                    { -1, titlePV }
                };


                // Add one file.
                var sourceFile = new SourceObjectFile
                {
                    SourceFilePath  = logFileTempFilePath,
                    Title           = newLogFileName, // For single-file-documents this is ignored.
                    Extension       = "txt"
                };

                // Create the new Log object for today, with name, eg "Log-2021-05-12", with automatic check-in after
                _vault.ObjectOperations.CreateNewSFDObject((int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument, propertyValues, sourceFile, CheckIn:true);

                return true;
            }
            catch (Exception ex)
            {
                _ = ex;

                return false;
            }
            finally
            {
                if (!String.IsNullOrEmpty(logFileTempFilePath) && File.Exists(logFileTempFilePath))
                {
                    File.Delete(logFileTempFilePath);
                }
            }
        }
    }
}
