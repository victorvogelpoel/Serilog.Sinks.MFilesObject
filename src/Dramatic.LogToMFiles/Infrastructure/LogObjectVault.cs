// LogObjectVault.cs
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
using System.Threading;
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    public class LogObjectVault : ILogObjectVault
    {
        private readonly IVault _vault;
        private readonly string _mfilesLogObjectTypeAlias;
        private readonly string _mfilesLogClassAlias;
        private readonly string _mfilesLogMessagePropDefAlias;
        private readonly Random _rnd = new Random();
        private int _mfilesLogObjectTypeID;
        private int _mfilesLogClassID;
        private int _mfilesLogMessagePropDefID;


        /// <summary>
        /// Construct the LogObjectVault for searching, appending and creating Log objects to save log messages
        /// </summary>
        /// <param name="vault"></param>
        /// <param name="mfilesLogObjectTypeAlias"></param>
        /// <param name="mfilesLogClassAlias"></param>
        /// <param name="mfilesLogMessagePropDefAlias"></param>
        public LogObjectVault(IVault vault,
                        string mfilesLogObjectTypeAlias     = DefaultLoggingVaultStructure.LogObjectTypeAlias,
                        string mfilesLogClassAlias          = DefaultLoggingVaultStructure.LogClassAlias,
                        string mfilesLogMessagePropDefAlias = DefaultLoggingVaultStructure.LogMessagePropertyDefinitionAlias)
        {
            if (String.IsNullOrWhiteSpace(mfilesLogObjectTypeAlias))        throw new ArgumentException($"{nameof(mfilesLogObjectTypeAlias)} cannot be null or empty; use something like \"OT.Serilog.MFilesObjectLogSink.Log\"", nameof(mfilesLogObjectTypeAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogClassAlias))             throw new ArgumentException($"{nameof(mfilesLogClassAlias)} cannot be null or empty; use something like \"CL.Serilog.MFilesObjectLogSink.Log\"", nameof(mfilesLogClassAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogMessagePropDefAlias))    throw new ArgumentException($"{nameof(mfilesLogMessagePropDefAlias)} cannot be null or empty; use something like \"PD.Serilog.MFilesObjectLogSink.LogMessage\"", nameof(mfilesLogMessagePropDefAlias));

            _vault                          = vault ?? throw new ArgumentNullException(nameof(vault));

            _mfilesLogObjectTypeAlias       = mfilesLogObjectTypeAlias;
            _mfilesLogClassAlias            = mfilesLogClassAlias;
            _mfilesLogMessagePropDefAlias   = mfilesLogMessagePropDefAlias;
        }


        private void ReadLoggingStructureIDs()
        {
            // Get the vault structure IDs for the aliases.
            // An operation call will return -1 when the alias cannot be found.

            _mfilesLogObjectTypeID      = _vault.ObjectTypeOperations.GetObjectTypeIDByAlias(_mfilesLogObjectTypeAlias);
            _mfilesLogClassID           = _vault.ClassOperations.GetObjectClassIDByAlias(_mfilesLogClassAlias);
            _mfilesLogMessagePropDefID  = _vault.PropertyDefOperations.GetPropertyDefIDByAlias(_mfilesLogMessagePropDefAlias);
        }


        /// <summary>
        /// Verify if logging structure is present in the vault.
        /// </summary>
        /// <returns></returns>
        public bool IsLogObjectStructurePresent()
        {
            ReadLoggingStructureIDs();

            return (_mfilesLogObjectTypeID != -1 && _mfilesLogClassID != -1 && _mfilesLogMessagePropDefID != -1);
        }


        /// <summary>
        /// Search the vault for logObjects that start with logObjectName
        /// </summary>
        /// <param name="logObjectBaseName">name of the Log object to search for</param>
        /// <returns>A list of ObjVer of the found Log objects</returns>
        public List<ObjVer> SearchLogObjects(string logObjectBaseName)
        {
            // Condition non-deleted objects
            var excludeDeletedItemSearchCondition = new SearchCondition();
            excludeDeletedItemSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);
            excludeDeletedItemSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            excludeDeletedItemSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);

            var mfilesLogObjectTypeID      = _vault.ObjectTypeOperations.GetObjectTypeIDByAlias(_mfilesLogObjectTypeAlias);

            // Condition ObjectType "Log"
            var otSearchCondition = new SearchCondition();
            otSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);
            otSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            otSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, mfilesLogObjectTypeID);

            // Condition NameOrTitle starts with "NamePrefix-todaysDate"
            var titleDefSearchCondition = new SearchCondition();
            titleDefSearchCondition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle, MFParentChildBehavior.MFParentChildBehaviorNone);
            // We want only Log objects which NameOrTitle begins with the search string provided.
            titleDefSearchCondition.ConditionType = MFConditionType.MFConditionTypeStartsWith;
            titleDefSearchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, logObjectBaseName);  // eg "Log-2021-05-12"

            var searchConditions = new SearchConditions
            {
                { -1, excludeDeletedItemSearchCondition },
                { -1, otSearchCondition },
                { -1, titleDefSearchCondition }
            };

            var searchResults = _vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions, MFSearchFlags.MFSearchFlagNone, SortResults: false);
            // return 0, 1, 2 or more Log objects for the current date, like "Log-2021-05-12", "Log-2021-05-12 (2)". They may not be sorted on title; we'll sort later on CreatedUtc

            if (searchResults.Count > 0 )
            {
                // Found one or more Log objects for today. Sort it on CreatedUtc.
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
                if (maxTestRetries > 0 && logObjectIsCheckedOut) { Thread.Sleep(millisecondsTimeout: _rnd.Next(200, 500)); }

            } while (maxTestRetries > 0 && logObjectIsCheckedOut);

            return !logObjectIsCheckedOut;
        }


        /// <summary>
        /// Read the Multi-line text property value from the specified Log object version.
        /// </summary>
        /// <param name="logObjVer">M-Files Object version to read the LogMessage from</param>
        /// <returns>The current value of the Multi-line text property value from the specified Log object version</returns>
        public string ReadLogMessageFromLogObject(ObjVer logObjVer)
        {
            var mfilesLogMessagePropDefID = _vault.PropertyDefOperations.GetPropertyDefIDByAlias(_mfilesLogMessagePropDefAlias);

            // Read the LogMessage MultiLineText prop value of the Log object
            return _vault.ObjectPropertyOperations.GetProperty(logObjVer, mfilesLogMessagePropDefID ).TypedValue.DisplayValue;
        }


        /// <summary>
        /// Write the logMessage to the specified existing Log object version.
        /// </summary>
        /// <param name="logObjVer">Log object version to write the logMessage to</param>
        /// <param name="logMessage">Log message to write to the Log object version</param>
        /// <returns>True if writing to the existing Log object version was successful, false otherwise</returns>
        /// <remarks>
        /// Possible failures writing the logMessage to the existing Log Object are:
        /// <list type="">
        ///     <item>couldn't read the propertyID for alias LogMessagePropDefAlias</item>
        ///     <item>the Log object version is checked out</item>
        ///     <item>the LogMessage property could not be set on the existing Log object version</item>
        ///     <item>the Log object version could not be checked in again</item>
        /// </list>
        /// </remarks>
        public bool WriteLogMessageToExistingLogObject(ObjVer logObjVer, string logMessage)
        {
            ObjectVersion checkedOutLogObjectVersion = null;

            try
            {
                var mfilesLogMessagePropDefID = _vault.PropertyDefOperations.GetPropertyDefIDByAlias(_mfilesLogMessagePropDefAlias);

                // Is the object not checked out? (and even test a number of retries with random waits); if checked out, simply create a new Log object with ordinal to store the batchedLogMessage
                if (IsObjectCheckedIn(logObjVer))
                {
                    // Check out the Log object and append the log message
                    checkedOutLogObjectVersion  = _vault.ObjectOperations.CheckOut(logObjVer.ObjID);

                    var logMessagePropValue = new PropertyValue { PropertyDef = mfilesLogMessagePropDefID };
                    logMessagePropValue.TypedValue.SetValue(MFDataType.MFDatatypeMultiLineText, logMessage);

                    _vault.ObjectPropertyOperations.SetProperty(checkedOutLogObjectVersion.ObjVer, logMessagePropValue);

                    // And at last, check in the existing Log object again.
                    _vault.ObjectOperations.CheckIn(checkedOutLogObjectVersion.ObjVer);

                    return true;
                }
                // else: the object is checked out even after retries, so we're going to create a new Log object for the batchedLogMessage
            }
            catch
            {
                // Any errors updating an existing Log object, then undo the checkout, and we'll try and create a new Log object for the batchedLogMessage instead
                if (null != checkedOutLogObjectVersion)
                {
                    _vault.ObjectOperations.UndoCheckout(checkedOutLogObjectVersion.ObjVer);
                }
            }

            return false;
        }


        /// <summary>
        /// Write the logMessage to a new Log object with a LogMessage property
        /// </summary>
        /// <param name="newLogObjectName">name for the new Log object; can contain an ordinal</param>
        /// <param name="logMessage">Log message to save to the new Log object</param>
        public bool WriteLogMessageToNewLogObject(string newLogObjectName, string logMessage)
        {
            try
            {
                // Class Log
                var classPV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass };
                classPV.Value.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogClassID);  // "Log" class

                var titlePV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle };
                titlePV.Value.SetValue(MFDataType.MFDatatypeText, newLogObjectName);     // eg "Log-2021-05-26", or "Log-2021-05-26 (2)"

                var logMessagePV = new PropertyValue { PropertyDef = _mfilesLogMessagePropDefID };  // 1159 = "LogMessage" multiline text
                logMessagePV.Value.SetValue(MFDataType.MFDatatypeMultiLineText, logMessage);

                var propertyValues = new PropertyValues
                {
                    { -1, classPV },
                    { -1, titlePV },
                    { -1, logMessagePV }
                };

                // Create the new Log object for the specified date, with name, eg "Log-2021-05-12", with automatic check-in after
                _vault.ObjectOperations.CreateNewObjectEx(_mfilesLogObjectTypeID, propertyValues, SourceFiles: null, SFD:false, CheckIn:true);

                return true;
            }
            catch (Exception ex)
            {
                _ = ex;

                return false;
            }
        }
    }


    /// <summary>
    /// Sort the search results on CreatedUtc
    /// </summary>
    internal class LogObjectCreatedComparer : IObjectComparer
    {
        public int Compare(ObjectVersion ObjectVersionDataLeft, ObjectVersion ObjectVersionDataRight)
        {
            if (ObjectVersionDataLeft is null)  { throw new ArgumentNullException(nameof(ObjectVersionDataLeft)); }
            if (ObjectVersionDataRight is null) { throw new ArgumentNullException(nameof(ObjectVersionDataRight)); }

            return (ObjectVersionDataLeft.CreatedUtc < ObjectVersionDataRight.CreatedUtc) ? -1 : 1;
        }
    }
}
