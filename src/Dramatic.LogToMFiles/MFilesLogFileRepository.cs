// MFilesLogRepository.cs
// 27-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    public class MFilesLogFileRepository
    {
        private readonly IVault _vault;


        private readonly string _mfilesLogFileNamePrefix;
        private readonly int _mfilesLogFileClassID;
        private readonly Random _rnd = new Random();

        /// <summary>
        ///
        /// </summary>
        /// <param name="vault">M-Files vault application</param>
        /// <param name="mfilesLogFileClassAlias">Alias for the Log ClassObject</param>
        /// <param name="controlledSwitch">Serilog switch to use for minimal log level</param>
        /// <param name="formatProvider"></param>
        public MFilesLogFileRepository( IVault vault,
                                        string mfilesLogFileNamePrefix    = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogObjectNamePrefix,
                                        string mfilesLogFileClassAlias      = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogFileClassAlias)
        {
            if (String.IsNullOrWhiteSpace(mfilesLogFileNamePrefix)) throw new ArgumentException($"{nameof(mfilesLogFileNamePrefix)} cannot be null or empty; use something like \"Log-\".", nameof(mfilesLogFileNamePrefix));
            if (String.IsNullOrWhiteSpace(mfilesLogFileClassAlias)) throw new ArgumentException($"{nameof(mfilesLogFileClassAlias)} cannot be null or empty; use something like \"PD.Serilog.MFilesObjectLogSink.LogFile\"", nameof(mfilesLogFileClassAlias));

            _vault                      = vault ?? throw new ArgumentNullException(nameof(vault));
            _mfilesLogFileNamePrefix    = mfilesLogFileNamePrefix;

            // Get the vault structure IDs for the aliases:
            _mfilesLogFileClassID       = vault.ClassOperations.GetObjectClassIDByAlias(mfilesLogFileClassAlias);

            // Health check
            if (_mfilesLogFileClassID == -1)
            {
                throw new InvalidOperationException($"Missing Log object structure in the vault. Run vault.EnsureLogSinkVaultStructure() with a MFilesObjectLogSinkVaultStructureConfiguration as an M-Files user with administrative permissions to create the logging vault structure.");
            }
        }


        /// <summary>
        /// Search for the Log object with prefix and date of today in the object NameOrTitle.
        /// Multiple found objects may be returned.
        /// </summary>
        /// <returns></returns>
        private ObjectSearchResults SearchTodaysLogFileDocuments()
        {
            var excludeDeletedItemSearchCondition = new SearchCondition();
            excludeDeletedItemSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);
            excludeDeletedItemSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            excludeDeletedItemSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);

            //// Search for ObjectType Document
            //var otSearchCondition = new SearchCondition();
            //otSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);
            //otSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            //otSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument);

            // Search for class LogFile
            var logFileClassSearchCondition = new SearchCondition();
            logFileClassSearchCondition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass, MFParentChildBehavior.MFParentChildBehaviorNone);
            logFileClassSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            logFileClassSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogFileClassID);

            var titleDefSearchCondition = new SearchCondition();
            titleDefSearchCondition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle, MFParentChildBehavior.MFParentChildBehaviorNone);

            // We want only LogFile class objects which NameOrTitle begins with the search string provided.
            titleDefSearchCondition.ConditionType = MFConditionType.MFConditionTypeStartsWith;

            titleDefSearchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, $"{_mfilesLogFileNamePrefix}{DateTime.Today:yyyy-MM-dd}");  // eg "Log-2021-05-12"

            var searchConditions    = new SearchConditions
            {
                { -1, excludeDeletedItemSearchCondition },
                { -1, logFileClassSearchCondition },
                { -1, titleDefSearchCondition }
            };

            var searchResults       = _vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions, MFSearchFlags.MFSearchFlagNone, SortResults: false);

            // return 0, 1 or more Log objects for the current date, like "Log-2021-05-12", "Log-2021-05-12 (2)". They may not be sorted on title; we'll sort later on CreatedUtc

            return searchResults;

        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="batchedLogMessage"></param>
        public void WriteLogFile(string batchedLogMessage)
        {
            // ObjectType 164 LOG
            // Class       95 LOG
            //   PropDef    1 Name or title - text

            // If nothing to in the message, then don't bother going further
            if (String.IsNullOrWhiteSpace(batchedLogMessage))       { return; }

            // Make sure the batched logMessage has a newline termination
            if (!batchedLogMessage.EndsWith(Environment.NewLine))   { batchedLogMessage += Environment.NewLine; }

            bool createNewLogObject     = true;

            var searchResults           = SearchTodaysLogFileDocuments();
            var existingLogObjectCount  = searchResults.Count;

            if (existingLogObjectCount > 0)
            {
                ObjectVersion checkedOutLogFileObjectVersion = null;

                // Found one or more Log objects for today. First sort it on CreatedUtc
                searchResults.Sort(new LogObjectCreatedComparer());

                // Work on the last item found, which is the last Log object that was created
                var lastLogObjVer = searchResults[existingLogObjectCount].ObjVer;

                try
                {
                    // Is the object checked out? (and even test a number of retries with random waits)
                    var lastLogObjectIsStillCheckedOut = IsObjectCheckedOut(lastLogObjVer.ObjID, maxRetries: 5);
                    if (!lastLogObjectIsStillCheckedOut)
                    {
                        // And get its file(s)
                        var currentLogFiles = _vault.ObjectFileOperations.GetFiles(lastLogObjVer);
                        if (currentLogFiles.Count == 1)
                        {
                            // Check out the LogFile object
                            checkedOutLogFileObjectVersion  = _vault.ObjectOperations.CheckOut(lastLogObjVer.ObjID);

                            // And get the future log file version
                            var checkedOutLogFiles          = _vault.ObjectFileOperations.GetFiles(checkedOutLogFileObjectVersion.ObjVer);

                            // Download file, add log message and upload again
                            AppendToLogObjectFile(batchedLogMessage, currentLogFiles[1], checkedOutLogFiles[1]);

                            // And at last, check in the existing Log object again.
                            _vault.ObjectOperations.CheckIn(checkedOutLogFileObjectVersion.ObjVer);

                            createNewLogObject     = false;
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
            }


            // If we failed before, then createNewLogObject is still true ==> just add a new LogFile object with the message as txt file
            if (createNewLogObject)
            {
                CreateNewLogObjectWithFile(batchedLogMessage, ++existingLogObjectCount);
            }
        }


        private void AppendToLogObjectFile(string logMessage, ObjectFile currentLogFile, ObjectFile updatedLogFile) // ObjID objID, FileVer fileVer)
        {
            string logFileTempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            _vault.ObjectFileOperations.DownloadFile(currentLogFile.ID, currentLogFile.Version, logFileTempFilePath);

            File.AppendAllText(logFileTempFilePath, logMessage, Encoding.UTF8);

            _vault.ObjectFileOperations.UploadFile(updatedLogFile.ID, updatedLogFile.Version, logFileTempFilePath);

        }


        /// <summary>
        /// For the logMessage, create a NEW Log object with title "{prefix}{DateTime.Now:yyyy-MM-dd}", eg "Log-2021-05-26"
        /// If the ordinal is more than 1, then a sequence number is added, eg "Log-2021-05-26 (2)"
        /// </summary>
        /// <param name="logMessage">a Log message with at most 10000 characters (MultiLineText limit)</param>
        /// <param name="logObjectOrdinal">the number of the new Log object to create. If larger than 1, this is added to the Log object title.</param>
        private void CreateNewLogObjectWithFile(string logMessage, int logObjectOrdinal)
        {
            string logFileTempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                // Write the logMessage to the temp file
                File.WriteAllText(logFileTempFilePath, logMessage, Encoding.UTF8);

                // Create a Log object title, with ordinal if needed, eg"Log-2021-05-26 (2)"
                var logObjectTitle = $"{_mfilesLogFileNamePrefix}{DateTime.Now:yyyy-MM-dd}";
                if (logObjectOrdinal > 1) { logObjectTitle += $" ({logObjectOrdinal})"; }

                // Class "LogFile"
                var classPV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass };
                classPV.Value.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogFileClassID);  // "LogFile" class

                // Prop "NameOrTitle"
                var titlePV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle };
                titlePV.Value.SetValue(MFDataType.MFDatatypeText, logObjectTitle);     // eg "Log-2021-05-26", or "Log-2021-05-26 (2)"

                var propertyValues = new PropertyValues
                {
                    { -1, classPV },
                    { -1, titlePV }
                };


                // Add one file.
                var sourceFile = new SourceObjectFile
                {
                    SourceFilePath  = logFileTempFilePath,
                    Title           = logObjectTitle, // For single-file-documents this is ignored.
                    Extension       = "txt"
                };

                // Create the new Log object for today, with name, eg "Log-2021-05-12", with automatic check-in after
                _vault.ObjectOperations.CreateNewSFDObject((int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument, propertyValues, sourceFile, CheckIn:true);

                //var sourceFiles = new SourceObjectFiles();
                //sourceFiles.AddFile(logObjectTitle, "txt", logFileTempFilePath);

                // Create the new Log object for today, with name, eg "Log-2021-05-12", with automatic check-in after
                //_vault.ObjectOperations.CreateNewObjectEx(_mfilesLogObjectTypeID, propertyValues, sourceFiles, SFD: true, CheckIn: true);
            }
            finally
            {
                if (!String.IsNullOrEmpty(logFileTempFilePath) && File.Exists(logFileTempFilePath))
                {
                    File.Delete(logFileTempFilePath);
                }
            }
        }





        /// <summary>
        /// Is the object checked out? We'll wait a number of retries with random wait and the last result is returned.
        /// </summary>
        /// <param name="logObjID">ID of object to test for checkout status</param>
        /// <param name="maxRetries">Number of retries to test the checkout status</param>
        /// <returns>true if the object is checked out, false if not</returns>
        private bool IsObjectCheckedOut(ObjID logObjID, int maxRetries)
        {
            bool logObjectIsCheckedOut;

            do
            {
                maxRetries--;

                logObjectIsCheckedOut = _vault.ObjectOperations.IsObjectCheckedOut(logObjID);
                if (maxRetries > 0 && logObjectIsCheckedOut) { Thread.Sleep(_rnd.Next(200, 1000)); }

            } while (maxRetries > 0 && logObjectIsCheckedOut);

            return logObjectIsCheckedOut;
        }
    }
}
