using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MFilesAPI;

namespace Serilog.Sinks.MFilesObject
{


    public class MFilesLogRepository
    {
        private readonly IVault _vault;

        public readonly string MFilesLogMessagePropertyDefinitionAlias;
        public readonly string MFilesLogObjectTypeAlias;
        public readonly string MFilesLogClassAlias;

        private readonly string _mfilesLogObjectNamePrefix;
        private readonly int _mfilesLogObjectTypeID;
        private readonly int _mfilesLogClassID;
        private readonly int _mfilesLogMessagePropDefID;

        private readonly Random _rnd = new Random();

        /// <summary>
        ///
        /// </summary>
        /// <param name="vault">M-Files vault application</param>
        /// <param name="mfilesLogObjectNamePrefix">Prefix for the Log object Name-or-Title. You may want to use the name of the VaultApplication.</param>
        /// <param name="mfilesLogObjectTypeAlias">Alias for the Log ObjectType</param>
        /// <param name="mfilesLogClassAlias">Alias for the Log ClassObject</param>
        /// <param name="mfilesLogMessagePropDefAlias">Alias for the LogMessage PropertyDefinition</param>
        /// <param name="controlledSwitch">Serilog switch to use for minimal log level</param>
        /// <param name="formatProvider"></param>
        public MFilesLogRepository(IVault vault, string mfilesLogObjectNamePrefix, string mfilesLogObjectTypeAlias, string mfilesLogClassAlias, string mfilesLogMessagePropDefAlias)
        {
            if (String.IsNullOrWhiteSpace(mfilesLogObjectNamePrefix))       throw new ArgumentException($"{nameof(mfilesLogObjectNamePrefix)} cannot be null or empty; use something like \"Log-\".", nameof(mfilesLogObjectNamePrefix));
            if (String.IsNullOrWhiteSpace(mfilesLogObjectTypeAlias))        throw new ArgumentException($"{nameof(mfilesLogObjectTypeAlias)} cannot be null or empty; use something like \"OT.Serilog.MFilesObjectLogSink.Log\"", nameof(mfilesLogObjectTypeAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogClassAlias))             throw new ArgumentException($"{nameof(mfilesLogClassAlias)} cannot be null or empty; use something like \"CL.Serilog.MFilesObjectLogSink.Log\"", nameof(mfilesLogClassAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogMessagePropDefAlias))    throw new ArgumentException($"{nameof(mfilesLogMessagePropDefAlias)} cannot be null or empty; use something like \"PD.Serilog.MFilesObjectLogSink.LogMessage\"", nameof(mfilesLogMessagePropDefAlias));

            _vault                                  = vault ?? throw new ArgumentNullException(nameof(vault));

            MFilesLogObjectTypeAlias                = mfilesLogObjectTypeAlias;
            MFilesLogClassAlias                     = mfilesLogClassAlias;
            MFilesLogMessagePropertyDefinitionAlias = mfilesLogMessagePropDefAlias;

            _mfilesLogObjectNamePrefix              = mfilesLogObjectNamePrefix;

            // Get the vault structure IDs for the aliases:
            _mfilesLogObjectTypeID                  = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(mfilesLogObjectTypeAlias);
            _mfilesLogClassID                       = vault.ClassOperations.GetObjectClassIDByAlias(mfilesLogClassAlias);
            _mfilesLogMessagePropDefID              = vault.PropertyDefOperations.GetPropertyDefIDByAlias(mfilesLogMessagePropDefAlias);
        }

        /// <summary>
        /// Search for the Log object with prefix and date of today in the object NameOrTitle.
        /// Multiple found objects may be returned.
        /// </summary>
        /// <returns></returns>
        private ObjectSearchResults SearchTodaysLogObjects()
        {
            var excludeDeletedItemSearchCondition = new SearchCondition();
            excludeDeletedItemSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);
            excludeDeletedItemSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            excludeDeletedItemSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);


            // Search for ObjectType "Log"
            var otSearchCondition = new SearchCondition();
            otSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);
            otSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            otSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogObjectTypeID);


            var titleDefSearchCondition = new SearchCondition();
            titleDefSearchCondition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle, MFParentChildBehavior.MFParentChildBehaviorNone);

            // We want only Log objects which NameOrTilt begins with the search string provided.
            titleDefSearchCondition.ConditionType = MFConditionType.MFConditionTypeStartsWith;

            titleDefSearchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, $"{_mfilesLogObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}");  // eg "Log-2021-05-12"

            var searchConditions    = new SearchConditions
            {
                { -1, excludeDeletedItemSearchCondition },
                { -1, otSearchCondition },
                { -1, titleDefSearchCondition }
            };

            var searchResults       = _vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions, MFSearchFlags.MFSearchFlagNone, SortResults: false);

            // return 0, 1 or more Log objects for the current date, like "Log-2021-05-12", "Log-2021-05-12 (2)". They may not be sorted on title; we'll sort later on CreatedUtc

            return searchResults;

        }


        /// <summary>
        /// Emit the batched log messages to one or more M-Files Log objects.
        ///
        /// If no Log object can be found for today, it will be created and at most 10.000 characters of the batched log message will be stored.
        /// If there is at least one existing Log object for today, then the batched log message will be appended to the LogMessage property of the last created Log object (up to 10.000 characters)
        /// Any remaining characters will be stored in one or more new Log Objects, up to 10.000 characters per object.
        /// </summary>
        /// <param name="batchedLogMessage">The log messages, with newline separators</param>
        public void WriteLogMessage(string batchedLogMessage)
        {
            // ObjectType 164 LOG
            // Class       95 LOG
            //   PropDef    1 Name or title - text
            //   PropDef 1159 LogMessage - multiline text


            // If nothing to in the message, then don't bother going further
            if (String.IsNullOrWhiteSpace(batchedLogMessage))       { return; }

            // Make sure the batched logMessage has a newline termination
            if (!batchedLogMessage.EndsWith(Environment.NewLine))   { batchedLogMessage += Environment.NewLine; }

            int index                   = 0;
            var maxLogObjectMessageSize = 10000;

            var searchResults           = SearchTodaysLogObjects();
            var existingLogObjectCount  = searchResults.Count;

            if (existingLogObjectCount > 0)
            {
                ObjectVersion checkedOutLogObjectVersion = null;

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
                        // Read the LogMessage MultiLineText prop of the Log object
                        var logMessagePV        = _vault.ObjectPropertyOperations.GetProperty(lastLogObjVer, _mfilesLogMessagePropDefID );

                        // Check out the Log object and append the log message
                        checkedOutLogObjectVersion = _vault.ObjectOperations.CheckOut(lastLogObjVer.ObjID);

                        // Get at most (maxLogObjectMessageSize - logMessagePV.TypedValue.DisplayValue.length) characters from the logMessage...
                        var logmessagePart = batchedLogMessage.TakeSubstringUpTotheLastSentence(index, maxLogObjectMessageSize - logMessagePV.TypedValue.DisplayValue.Length);

                        // ... and save this with the existing log Object
                        logMessagePV.TypedValue.SetValue(MFDataType.MFDatatypeMultiLineText, $"{logMessagePV.TypedValue.DisplayValue}{logmessagePart}");
                        _vault.ObjectPropertyOperations.SetProperty(checkedOutLogObjectVersion.ObjVer, logMessagePV);

                        // And at last, check in the existing Log object again.
                        _vault.ObjectOperations.CheckIn(checkedOutLogObjectVersion.ObjVer);

                        // Advance the index into the batchedLogMessage until we have written it all.
                        index += logmessagePart.Length;

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
            }


            // If we have still characters left in the batchedLogMessage, then create 1 or more Log objects for it
            while (index < batchedLogMessage.Length)
            {
                // Get at most (maxLogObjectMessageSize - logMessagePV.TypedValue.DisplayValue.length) characters from the logMessage...
                var logmessagePart = batchedLogMessage.TakeSubstringUpTotheLastSentence(index, Math.Min(maxLogObjectMessageSize, batchedLogMessage.Length-index));

                CreateNewLogObjectWithLogMessage(logmessagePart, ++existingLogObjectCount);

                // Advance the index into the batchedLogMessage until we have written it all.
                index += logmessagePart.Length;
            }
        }


        /// <summary>
        /// For the logMessage, create a NEW Log object with title "{prefix}{DateTime.Now:yyyy-MM-dd}", eg "Log-2021-05-26"
        /// If the ordinal is more than 1, then a sequence number is added, eg "Log-2021-05-26 (2)"
        /// </summary>
        /// <param name="logMessage">a Log message with at most 10000 characters (MultiLineText limit)</param>
        /// <param name="logObjectOrdinal">the number of the new Log object to create. If larger than 1, this is added to the Log object title.</param>
        private void CreateNewLogObjectWithLogMessage(string logMessage, int logObjectOrdinal)
        {
            var logObjectTitle = $"{_mfilesLogObjectNamePrefix}{DateTime.Now:yyyy-MM-dd}";

            if (logObjectOrdinal > 1) { logObjectTitle += $" ({logObjectOrdinal})"; }


            // Class Log
            var classPV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass };
            classPV.Value.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogClassID);  // "Log" class

            var titlePV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle };
            titlePV.Value.SetValue(MFDataType.MFDatatypeText, logObjectTitle);     // eg "Log-2021-05-26", or "Log-2021-05-26 (2)"

            var logMessagePV = new PropertyValue { PropertyDef = _mfilesLogMessagePropDefID };  // 1159 = "LogMessage" multiline text
            logMessagePV.Value.SetValue(MFDataType.MFDatatypeMultiLineText, logMessage);

            var propertyValues = new PropertyValues
            {
                { -1, classPV },
                { -1, titlePV },
                { -1, logMessagePV }
            };

            // Create the new Log object for today, with name, eg "Log-2021-05-12", with automatic check-in after
            _vault.ObjectOperations.CreateNewObjectEx(_mfilesLogObjectTypeID, propertyValues, SourceFiles: null, SFD:false, CheckIn:true);
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

    /// <summary>
    /// Sort the search results on CreatedUtc
    /// </summary>
    internal class LogObjectCreatedComparer : IObjectComparer
    {
        public int Compare(ObjectVersion objectVersionDataLeft, ObjectVersion objectVersionDataRight)
        {
            if (objectVersionDataLeft is null)  { throw new ArgumentNullException(nameof(objectVersionDataLeft)); }
            if (objectVersionDataRight is null) { throw new ArgumentNullException(nameof(objectVersionDataRight)); }

            return (objectVersionDataLeft.CreatedUtc < objectVersionDataRight.CreatedUtc) ? -1 : 1;
        }
    }


    public static class StringExtensions
    {
        /// <summary>
        /// Take at most maxCharactersToTake from the source string from index and return up to the last whole sentence.
        /// If no newline is found, maxCharactersToTake characters are returned from the index location in the sourceString.
        /// </summary>
        /// <param name="sourceString">The string to take sentences from</param>
        /// <param name="index">the location to take the sentences from</param>
        /// <param name="maxCharactersToTake">The maximum number of characters to take.</param>
        /// <returns></returns>
        public static string TakeSubstringUpTotheLastSentence(this string sourceString, int index, int maxCharactersToTake)
        {
            int newLineCharSize         = Environment.NewLine.Length;
            var substring               = sourceString.Substring(index, Math.Min(sourceString.Length-index, maxCharactersToTake));

            var lastNLOccurrence        = substring.LastIndexOf(Environment.NewLine, StringComparison.Ordinal);
            if (lastNLOccurrence > 0 && lastNLOccurrence+newLineCharSize < substring.Length)
            {
                // NL found and clip the substring, including the NewLine.
                substring = substring.Substring(0, lastNLOccurrence+newLineCharSize);
            }

            return substring;
        }
    }
}
