// MFilesObjectLogSink.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MFilesAPI;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MFilesObject
{
    public class MFilesObjectLogSink : IBatchedLogEventSink
    {
        internal const string DefaultMFilesLogMessagePropertyDefinitionAlias    = "PD.Serilog.MFilesObjectLogSink.LogMessage";
        internal const string DefaultMFilesLogObjectTypeAlias                   = "OT.Serilog.MFilesObjectLogSink.Log";
        internal const string DefaultMFilesLogClassAlias                        = "CL.Serilog.MFilesObjectLogSink.Log";
        internal const string DefaultMFilesLogObjectNamePrefix                  = "Log-";

        public const int DefaultBatchPostingLimit                               = 1000;
        public const int DefaultQueueSizeLimit                                  = 100000;
        public static readonly TimeSpan DefaultPeriod                           = TimeSpan.FromSeconds(5);
        static readonly TimeSpan RequiredLevelCheckInterval                     = TimeSpan.FromMinutes(2);
        private DateTime _nextRequiredLevelCheckUtc                             = DateTime.UtcNow.Add(RequiredLevelCheckInterval);
        private readonly ControlledLevelSwitch _controlledSwitch;


        private readonly IFormatProvider _formatProvider;
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
        public MFilesObjectLogSink(IVault vault, string mfilesLogObjectNamePrefix, string mfilesLogObjectTypeAlias, string mfilesLogClassAlias, string mfilesLogMessagePropDefAlias, ControlledLevelSwitch controlledSwitch, IFormatProvider formatProvider)
        {
            if (String.IsNullOrWhiteSpace(mfilesLogObjectNamePrefix))       throw new ArgumentException($"{nameof(mfilesLogObjectNamePrefix)} cannot be null or empty; use something like \"Log-\".", nameof(mfilesLogObjectNamePrefix));
            if (String.IsNullOrWhiteSpace(mfilesLogObjectTypeAlias))        throw new ArgumentException($"{nameof(mfilesLogObjectTypeAlias)} cannot be null or empty; use something like \"{DefaultMFilesLogObjectTypeAlias}\"", nameof(mfilesLogObjectTypeAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogClassAlias))             throw new ArgumentException($"{nameof(mfilesLogClassAlias)} cannot be null or empty; use something like \"{DefaultMFilesLogMessagePropertyDefinitionAlias}\"", nameof(mfilesLogClassAlias));
            if (String.IsNullOrWhiteSpace(mfilesLogMessagePropDefAlias))    throw new ArgumentException($"{nameof(mfilesLogMessagePropDefAlias)} cannot be null or empty; use something like \"{DefaultMFilesLogClassAlias}\"", nameof(mfilesLogMessagePropDefAlias));

            _vault                                  = vault ?? throw new ArgumentNullException(nameof(vault));
            _controlledSwitch                       = controlledSwitch ?? throw new ArgumentNullException(nameof(controlledSwitch));
            _formatProvider                         = formatProvider;

            MFilesLogObjectTypeAlias                = mfilesLogObjectTypeAlias;
            MFilesLogClassAlias                     = mfilesLogClassAlias;
            MFilesLogMessagePropertyDefinitionAlias = mfilesLogMessagePropDefAlias;

            _mfilesLogObjectNamePrefix              = mfilesLogObjectNamePrefix;

            // Get the vault structure IDs for the aliases:
            _mfilesLogObjectTypeID                  = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(mfilesLogObjectTypeAlias);
            _mfilesLogClassID                       = vault.ClassOperations.GetObjectClassIDByAlias(mfilesLogClassAlias);
            _mfilesLogMessagePropDefID              = vault.PropertyDefOperations.GetPropertyDefIDByAlias(mfilesLogMessagePropDefAlias);
        }

        //public async Task OnEmptyBatchAsync()
        //{
        //    if (_controlledSwitch.IsActive && _nextRequiredLevelCheckUtc < DateTime.UtcNow)
        //    {
        //        await EmitBatchAsync(Enumerable.Empty<LogEvent>());
        //    }
        //}

        public Task OnEmptyBatchAsync()
        {
            return Task.FromResult(0);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="batch"></param>
        /// <returns></returns>
        public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            var batchedMessage = new StringBuilder();
            foreach (var logEvent in batch.Where(logEvent => logEvent.Level != LogEventLevel.Debug && logEvent.Level != LogEventLevel.Verbose))
            {
                batchedMessage.AppendLine($"{logEvent.Timestamp.ToString("HH:mm:ss")} [{logEvent.Level.ToString().ToUpperInvariant()}] {logEvent.RenderMessage(_formatProvider)}");
            }

            try
            {
                EmitToMFilesLogObject(batchedMessage.ToString());
            }
            catch (Exception ex)
            {
                throw;  // Keeping this to help me debug M-Files / Serilog exceptions; TODO: remove the catch at sink release.
            }

            return Task.FromResult(0);
        }



        /// <summary>
        /// Emit the batched log messages to the M-Files Log object.
        /// If there is no Log object for today, it will be created.
        /// If there is an existing Log object for today, then the batched messsages will be appended to the LogMessage property
        /// </summary>
        /// <param name="batchedLogMessage"></param>
        public void EmitToMFilesLogObject(string batchedLogMessage)
        {


            // If nothing to in the message, then don't bother emitting the message to the M-Files object
            if (String.IsNullOrWhiteSpace(batchedLogMessage)) return;

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
            // We want to search by property - in this case the built-in "name or title" property.
            // Alternatively we could pass the ID of the property definition if it's not built-in.
            titleDefSearchCondition.Expression.SetPropertyValueExpression((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle, MFParentChildBehavior.MFParentChildBehaviorNone);

            // We want only items that equal the search string provided.
            titleDefSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;

            // We want to search for items that are named "hello world".
            // Note that the type must both match the property definition type, and be applicable for the
            // supplied value.
            titleDefSearchCondition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, $"{_mfilesLogObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}");  // eg "Log-2021-05-12"

            var searchConditions = new SearchConditions();
            searchConditions.Add(-1, excludeDeletedItemSearchCondition);
            searchConditions.Add(-1, otSearchCondition);
            searchConditions.Add(-1, titleDefSearchCondition);

            // Find the Log object per day, eg "Log-yyyy-MM-dd"
            // ObjectType 164 LOG
            // Class       95 LOG
            //   PropDef    1 Name or title - text
            //   PropDef 1159 LogMessage - multiline text


            var searchResults       = _vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions, MFSearchFlags.MFSearchFlagNone, SortResults: false);
            var createNewLogObject  = (searchResults.Count == 0);

            if (!createNewLogObject)
            {
                // Found a LOG object for today, so APPEND the message to the existing LogMessage contents. Do not create a new Log object for today.
                ObjectVersion checkedOutObjectVersion = null;

                try
                {
                    // Work on the first item found
                    var existingLogObjID = searchResults[1].ObjVer.ObjID;

                    // If Todays Log object is checked out, then wait a random time for a number of retries
                    var logObjectIsCheckedOut   = false;
                    var maxRetries              = 5;
                    do
                    {
                        maxRetries--;

                        logObjectIsCheckedOut = _vault.ObjectOperations.IsObjectCheckedOut(existingLogObjID);
                        if (maxRetries > 0 && logObjectIsCheckedOut) { Thread.Sleep(_rnd.Next(200, 1000)); }

                    } while (maxRetries > 0 && logObjectIsCheckedOut);


                    if (logObjectIsCheckedOut)
                    {
                        createNewLogObject = true;
                    }
                    else
                    {
                        // Check out the Log object and append the log message
                        checkedOutObjectVersion = _vault.ObjectOperations.CheckOut(existingLogObjID);

                        // Get the LogMessage property of the existing Log object and update it with the batched log messages
                        var logMessagePV = _vault.ObjectPropertyOperations.GetProperty(checkedOutObjectVersion.ObjVer, _mfilesLogMessagePropDefID );  // 1159 = "LogMessage" multiline text
                        logMessagePV.TypedValue.SetValue(MFDataType.MFDatatypeMultiLineText, $"{logMessagePV.TypedValue.DisplayValue}{batchedLogMessage}");
                        _vault.ObjectPropertyOperations.SetProperty(checkedOutObjectVersion.ObjVer, logMessagePV);

                        _vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);
                    }
                }
                catch
                {
                    // Any errors, then undo the checkout
                    if (null != checkedOutObjectVersion)
                    {
                        _vault.ObjectOperations.UndoCheckout(checkedOutObjectVersion.ObjVer);
                    }

                    // So we have an error with an existing Log Object; now create a NEW Log object to store the batchedLogmessage.
                    createNewLogObject = true;
                }
            }

            // OK, no Log object existed with todays name, or one existed but could not be checked out and we're creating a NEW Log object.
            if (createNewLogObject)
            {
                // No Log object found for today, so create a new Log object.

                // Class Log
                var classPV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass };
                classPV.Value.SetValue(MFDataType.MFDatatypeLookup, _mfilesLogClassID);  // "Log" class;

                var titlePV = new PropertyValue { PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle };
                titlePV.Value.SetValue(MFDataType.MFDatatypeText, $"{_mfilesLogObjectNamePrefix}{DateTime.Now:yyyy-MM-dd}");     // eg "Log-2021-05-12"

                var logMessagePV = new PropertyValue { PropertyDef = _mfilesLogMessagePropDefID };  // 1159 = "LogMessage" multiline text
                logMessagePV.Value.SetValue(MFDataType.MFDatatypeMultiLineText, batchedLogMessage);

                var propertyValues = new PropertyValues();
                propertyValues.Add(-1, classPV);
                propertyValues.Add(-1, titlePV);
                propertyValues.Add(-1, logMessagePV);

                // Create the new Log object for today, with name, eg "Log-2021-05-12", with automatic check-in after
                var newLogObjectVersion = _vault.ObjectOperations.CreateNewObjectEx(_mfilesLogObjectTypeID, propertyValues, SourceFiles: null, SFD:false, CheckIn:true);
            }
        }
    }
}

