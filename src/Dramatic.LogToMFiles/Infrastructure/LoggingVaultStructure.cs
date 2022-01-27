// LoggingVaultStructure.cs
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
using System.Collections.Generic;
using System.Linq;
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    /// <summary>Default names and aliases for the logging vault structure</summary>
    public static class DefaultLoggingVaultStructure
    {
        /// <summary>Singular name of the M-Files Log object type, default "Log"</summary>
        public const string LogObjectTypeNameSingular               = "Log";
        /// <summary>Plural name of the M-Files Log object type, default "Logs"</summary>
        public const string LogObjectTypeNamePlural                 = "Logs";
        /// <summary>Name of the M-Files LogMessage property definition, default "LogMessage"</summary>
        public const string LogMessagePropDefName                   = "LogMessage";

        /// <summary>Alias for the M-Files Log object type, default "OT.Serilog.MFilesObjectLogSink.Log"</summary>
        public const string LogObjectTypeAlias                      = "OT.Serilog.MFilesObjectLogSink.Log";
        /// <summary>Alias for the M-Files class for the Log class, default "CL.Serilog.MFilesObjectLogSink.Log"</summary></summary>
        public const string LogClassAlias                           = "CL.Serilog.MFilesObjectLogSink.Log";
        /// <summary>Alias for the LogMessage property definition, default "PD.Serilog.MFilesObjectLogSink.LogMessage"</summary>
        public const string LogMessagePropertyDefinitionAlias       = "PD.Serilog.MFilesObjectLogSink.LogMessage";
        /// <summary>Prefix string for the NameOrTitle of the Log Object document object, eg "Log-" results in Log object with name "Log-2021-07-18", "Log-2021-07-18 (2)", etc </summary>
        public const string LogObjectNamePrefix                     = "Log-";

        /// <summary>Alias for the Log file document class, default "CL.Serilog.MFilesObjectLogSink.LogFile"</summary>
        public const string LogFileClassName                        = "LogFile";
        /// <summary>Name of the M-Files class for the Log file document object (with OT Document)</summary>
        public const string LogFileClassAlias                       = "CL.Serilog.MFilesObjectLogSink.LogFile";
        /// <summary>Prefix string for the NameOrTitle of the Log File document object, eg "Log-", which results in "Log-2021-07-18.txt"</summary>
        public const string LogFileNamePrefix                       = "Log-";
    }


    /// <summary>
    /// Configuration for the logging vault structure
    /// </summary>
    public class LoggingVaultStructureConfiguration
    {
        /// <summary>Singular name of the M-Files Log object type, default "Log"</summary>
        public string LogObjectTypeNameSingular { get; set; }       = DefaultLoggingVaultStructure.LogObjectTypeNameSingular;
        /// <summary>Plural name of the M-Files Log object type, default "Logs"</summary>
        public string LogObjectTypeNamePlural   { get; set; }       = DefaultLoggingVaultStructure.LogObjectTypeNamePlural;
        /// <summary>Name of the M-Files LogMessage property definition, default "LogMessage"</summary>
        public string LogMessagePropDefName     { get; set; }       = DefaultLoggingVaultStructure.LogMessagePropDefName;

        /// <summary>Alias for the M-Files Log object type, default "OT.Serilog.MFilesObjectLogSink.Log"</summary>
        public string LogObjectTypeAlias        { get; set; }       = DefaultLoggingVaultStructure.LogObjectTypeAlias;
        /// <summary>Alias for the M-Files class for the Log class, default "CL.Serilog.MFilesObjectLogSink.Log"</summary></summary>
        public string LogClassAlias             { get; set; }       = DefaultLoggingVaultStructure.LogClassAlias;
        /// <summary>Alias for the LogMessage property definition, default "PD.Serilog.MFilesObjectLogSink.LogMessage"</summary>
        public string LogMessagePropDefAlias    { get; set; }       = DefaultLoggingVaultStructure.LogMessagePropertyDefinitionAlias;

        /// <summary>Name of the M-Files class for the Log file document object (with OT Document)</summary>
        public string LogFileClassName          { get; set; }       = DefaultLoggingVaultStructure.LogFileClassName;
        /// <summary>Alias for the Log file document class, default "CL.Serilog.MFilesObjectLogSink.LogFile"</summary>
        public string LogFileClassAlias         { get; set; }       = DefaultLoggingVaultStructure.LogFileClassAlias;
    }


    /// <summary>
    ///
    /// </summary>
    public static class LoggingVaultStructure
    {
        /// <summary>Lock object for mutexing structure changings and log writing</summary>
        public static readonly object StructureChangeLock                   = new Object();


        /// <summary>
        /// Gets the missing logging vault structure in vault.
        /// </summary>
        /// <remarks>
        /// Tests if the following aliases can be resolved: LogObjectTypeAlias, LogClassAlias, LogMessagePropDefAlias, LogFileClassAlias and returns the missing ones
        /// </remarks>
        /// <param name="vault">Vault to test for the logging structure</param>
        /// <returns>a List of the logging structure that is missing in the vault</returns>
        public static List<String> GetMissingLoggingVaultStructure(this IVault vault, string logObjectTypeAlias, string logClassAlias, string logMessagePropDefAlias, string logFileClassAlias)
        {
            if (vault is null)              { throw new ArgumentNullException(nameof(vault)); }

            var expectedAliases = new List<String>() { logObjectTypeAlias, logClassAlias, logMessagePropDefAlias, logFileClassAlias};
            var presentAliases  = new List<String>();

            lock(StructureChangeLock)
            {
                if (vault.ObjectTypeOperations.GetObjectTypeIDByAlias(logObjectTypeAlias) != -1)        { presentAliases.Add(logObjectTypeAlias); }
                if (vault.ClassOperations.GetObjectClassIDByAlias(logClassAlias) != -1)                 { presentAliases.Add(logClassAlias); }
                if (vault.PropertyDefOperations.GetPropertyDefIDByAlias(logMessagePropDefAlias) != -1)  { presentAliases.Add(logMessagePropDefAlias); }
                if (vault.ClassOperations.GetObjectClassIDByAlias(logFileClassAlias) != -1)             { presentAliases.Add(logFileClassAlias); }

                return expectedAliases.Except(presentAliases, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }



        /// <summary>
        /// Test if the logging structure is present in the vault
        /// </summary>
        /// <param name="vault"></param>
        /// <param name="structureConfig"></param>
        /// <returns></returns>
        public static bool HasLoggingVaultStructure(this IVault vault, LoggingVaultStructureConfiguration structureConfig)
        {
            if (vault is null)              { throw new ArgumentNullException(nameof(vault)); }
            if (structureConfig is null)    { throw new ArgumentNullException(nameof(structureConfig)); }

            lock(StructureChangeLock)
            {
                // Get the vault structure IDs for the aliases:
                var mfilesLogObjectTypeID       = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(structureConfig.LogObjectTypeAlias);
                var mfilesLogClassID            = vault.ClassOperations.GetObjectClassIDByAlias(structureConfig.LogClassAlias);
                var mfilesLogMessagePropDefID   = vault.PropertyDefOperations.GetPropertyDefIDByAlias(structureConfig.LogMessagePropDefAlias);
                var mfilesLogFileClassID        = vault.ClassOperations.GetObjectClassIDByAlias(structureConfig.LogFileClassAlias);

                return mfilesLogObjectTypeID != -1 && mfilesLogClassID != -1 && mfilesLogMessagePropDefID != -1 && mfilesLogFileClassID != -1;
            }
        }


        /// <summary>
        /// Ensure the structure in the M-Files vault needed for logging; full control permissions are necessary to create or update structure.
        /// </summary>
        /// <remarks>
        /// An <see cref="InvalidOperationException"/> will be thrown if you attempt to use this method on the PermanentVault, as this is a cached structure vault.
        /// </remarks>
        /// <param name="vault">Reference to the M-Files vault; make sure you're connected to the vault with full control permissions. Do NOT specify the PermanentVault.</param>
        /// <param name="structureConfig">Settings for creating Log structure in the vault.</param>
        public static void EnsureLoggingVaultStructure(this IVault vault, LoggingVaultStructureConfiguration structureConfig)
        {
            // Add OT "Log" with CL "Log"
            // Add PD "LogMessage"

            if (vault is null)                                          { throw new ArgumentNullException(nameof(vault)); }
            if (vault.GetType().FullName == "MetadataCacheVault_Dyn")   { throw new InvalidOperationException($"The PermanentVault reference cannot be used. It is a vault with cached structure and will yield structural lookup errors when creating the logging structure."); }
            if (structureConfig is null)                                { throw new ArgumentNullException(nameof(structureConfig)); }

            lock(StructureChangeLock)
            {
                var logObjectTypeID                     = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(structureConfig.LogObjectTypeAlias);
                var logMessagePropDefID                 = vault.PropertyDefOperations.GetPropertyDefIDByAlias(structureConfig.LogMessagePropDefAlias);
                var logFileClassID                      = vault.ClassOperations.GetObjectClassIDByAlias(structureConfig.LogFileClassAlias);

                var allInternalUsersGroupID            = 1;
                var allInternalAndExternalUsersGroupID = 2;

                // Add Log ObjectType if it doesn't exist
                if (logObjectTypeID == -1)
                {
                    // Construct an access list for the ObjectType
                    var ace = new AccessControlEntryContainer();

                    // "All internal and external users"
                    var acek = new AccessControlEntryKey();
                    acek.SetUserOrGroupID(allInternalAndExternalUsersGroupID, IsGroup:true);
                    var aced = new AccessControlEntryData
                    {
                        ReadPermission = MFPermission.MFPermissionAllow
                    };
                    ace.Add(acek, aced);

                    // "All internal users"
                    acek = new AccessControlEntryKey();
                    acek.SetUserOrGroupID(allInternalUsersGroupID, IsGroup:true);
                    aced = new AccessControlEntryData
                    {
                        ReadPermission = MFPermission.MFPermissionAllow,
                        EditPermission = MFPermission.MFPermissionAllow
                    };
                    ace.Add(acek, aced);

                    // Bind the entries container to the access control list.
                    var acl = new AccessControlList();
                    acl.CustomComponent.AccessControlEntries = ace;

                    ObjType objType = new ObjType()
                    {
                        NameSingular                    = structureConfig.LogObjectTypeNameSingular,
                        NamePlural                      = structureConfig.LogObjectTypeNamePlural,
                        AllowAdding                     = true,
                        AllowedAsGroupingLevel          = false,
                        RealObjectType                  = true,
                        CanHaveFiles                    = false,
                        External                        = false,
                        ShowCreationCommandInTaskPane   = false,
                        Hierarchical                    = false,
                        HasOwnerType                    = false,
                        OwnerType                       = 0,
                        Translatable                    = false,
                        AccessControlList               = acl
                        //Icon                            =
                    };

                    var objTypeAdmin = new ObjTypeAdmin()
                    {
                        ObjectType                      = objType,
                        SemanticAliases                 = new SemanticAliases() { Value = structureConfig.LogObjectTypeAlias }
                    };

                    var newObjTypeAdmin = vault.ObjectTypeOperations.AddObjectTypeAdmin(objTypeAdmin);
                    logObjectTypeID     = newObjTypeAdmin.ObjectType.ID;
                }

                // Add LogMessage propertyDef if it doesn't exist
                if (logMessagePropDefID == -1)
                {
                    // Construct an access list for the PropertyDef
                    var ace = new AccessControlEntryContainer();

                    var acek = new AccessControlEntryKey();
                    acek.SetUserOrGroupID(allInternalAndExternalUsersGroupID, IsGroup:true);
                    var aced = new AccessControlEntryData
                    {
                        ReadPermission = MFPermission.MFPermissionAllow,
                        EditPermission = MFPermission.MFPermissionAllow
                    };
                    ace.Add(acek, aced);

                    // Bind the entries container to the access control list.
                    var acl = new AccessControlList();
                    acl.CustomComponent.AccessControlEntries = ace;


                    var propDef = new PropertyDef
                    {
                        Name                            = structureConfig.LogMessagePropDefName,
                        DataType                        = MFDataType.MFDatatypeMultiLineText,
                        ContentType                     = MFContentType.MFContentTypeGeneric,
                        ObjectType                      = logObjectTypeID,                              // Either ID of the newly created ObjectType or the found-by-alias ObjectType
                        BasedOnValueList                = false,
                        AllObjectTypes                  = false,
                        Predefined                      = false,
                        ObjectsSearchableByThisProperty = false,
                        HistoryVersionsSearchableByThisProperty = false,
                        AllowedAsGroupingLevel          = false,
                        SortAscending                   = true,
                        FormattingType                  = MFFormattingType.MFFormattingTypeNone,
                        AutomaticValueType              = MFAutomaticValueType.MFAutomaticValueTypeNone,
                        ValidationType                  = MFValidationType.MFValidationTypeNone,
                        UpdateType                      = MFUpdateType.MFUpdateTypeNormal,
                        AccessControlList               = acl
                    };

                    var propDefAdmin = new PropertyDefAdmin()
                    {
                        PropertyDef                     = propDef,
                        SemanticAliases                 = new SemanticAliases() { Value = structureConfig.LogMessagePropDefAlias }
                    };

                    var newPropertyDefAdmin = vault.PropertyDefOperations.AddPropertyDefAdmin(propDefAdmin);
                    logMessagePropDefID     = newPropertyDefAdmin.PropertyDef.ID;
                }

                // Now get the associated classes for the object type (should be 1)
                var classesForLogObjectType =    vault.ClassOperations.GetObjectClassesAdmin(logObjectTypeID);
                if (classesForLogObjectType.Count != 1) { throw new InvalidOperationException($"ObjectTtype with alias \"{structureConfig.LogObjectTypeAlias}\" should have only 1 class associated, found {classesForLogObjectType.Count}."); }

                var classForLogObjectType       = classesForLogObjectType[1];
                var classAliases                = ";" + classForLogObjectType.SemanticAliases.Value.Trim() + ";";               // ";;", ";CL.SomeAlias;" or ";OT.Serilog.MFilesObjectLogSink.Log;"
                var classAlreadyHasLoggingAlias = classAliases.Contains(";" + structureConfig.LogClassAlias.Trim() + ";");      // test if ";OT.Serilog.MFilesObjectLogSink.Log; is part of the alias => class Log was already configured.

                if (!classAlreadyHasLoggingAlias)
                {
                    // Class for object type has not been updated yet, so add LogMessage PD and alias

                    var associatedLogMessagePropDef = new AssociatedPropertyDef
                    {
                        PropertyDef                     = logMessagePropDefID,
                        Required                        = false
                    };

                    classForLogObjectType.AssociatedPropertyDefs.Add(-1, associatedLogMessagePropDef);

                    // Update Alias, appending to existing alias
                    classForLogObjectType.SemanticAliases.Value = (classForLogObjectType.SemanticAliases.Value.Trim(new char[] {' ', ';' }) + ";" + structureConfig.LogClassAlias.Trim()).Trim(';');

                    vault.ClassOperations.UpdateObjectClassAdmin(classForLogObjectType);
                }

                // Add LogFile class for the second Serilog sink, if not already existing
                // Add CL "LogFile" for OT "Document"
                if (logFileClassID == -1)
                {
                    // OT Document
                    // CL LogFile

                    var associatedNamePropDef = new AssociatedPropertyDef
                    {
                        PropertyDef = 0, // NameOrTitle
                        Required = true
                    };

                    var associatedPropDefs = new AssociatedPropertyDefs
                    {
                        { -1, associatedNamePropDef },
                    };

                    var objectClassAdmin = new ObjectClassAdmin
                    {
                        Name                    = structureConfig.LogFileClassName,
                        ObjectType              = (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument,
                        NamePropertyDef         = 0,    // NameOrTitle PropertyDef
                        AssociatedPropertyDefs  = associatedPropDefs,
                        ForceWorkflow           = false,

                        SemanticAliases         = new SemanticAliases() { Value = structureConfig.LogFileClassAlias }
                    };

                    vault.ClassOperations.AddObjectClassAdmin(objectClassAdmin);
                }
            }
        }


        /// <summary>
        /// Destroy Logging structure from the vault after we first have destroyed all Log objects and LogFile objects.
        /// </summary>
        /// <remarks>
        /// An <see cref="InvalidOperationException"/> will be thrown if you attempt to use this method on the PermanentVault, as this is a cached structure vault.
        /// </remarks>
        /// <param name="vault">Reference to the M-Files vault; make sure you're connected to the vault with full control permissions. Do NOT specify the PermanentVault.</param>
        /// <param name="structureConfig">Settings for creating Log structure in the vault.</param>
        public static void RemoveLogObjectsAndLoggingVaultStructure(this IVault vault, LoggingVaultStructureConfiguration structureConfig)
        {
            if (vault is null)                                          { throw new ArgumentNullException(nameof(vault)); }
            if (vault.GetType().FullName == "MetadataCacheVault_Dyn")   { throw new InvalidOperationException($"The PermanentVault reference cannot be used. It is a vault with cached structure and will yield structural lookup errors when creating the logging structure."); }

            lock(StructureChangeLock)
            {
                DestroyAllLogObjectsAndVaultStructure(vault, structureConfig);

                DestroyAllLogFileObjectsAndVaultStructure(vault, structureConfig);
            }
        }


        /// <summary>
        /// Destroy all objects and vault structure for Log objectType
        /// </summary>
        /// <param name="vault">vault to destroy in</param>
        /// <param name="structureConfig">configuration containing aliases of structure to remove</param>
        private static void DestroyAllLogObjectsAndVaultStructure(IVault vault, LoggingVaultStructureConfiguration structureConfig)
        {
            var mfilesLogObjectTypeID = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(structureConfig.LogObjectTypeAlias);
            if (mfilesLogObjectTypeID == -1) { return; }

            bool moreResults;
            do
            {
                var logObjectSearchResults = SearchAllLogObjects(vault, mfilesLogObjectTypeID);
                moreResults                = logObjectSearchResults.MoreResults;

                foreach(ObjectVersion logObjectVersion in logObjectSearchResults)
                {
                    vault.ObjectOperations.DestroyObject(logObjectVersion.ObjVer.ObjID, DestroyAllVersions:true, ObjectVersion:-1);
                }
            }
            while (moreResults);

            // OT Log (and CL Log)
            // PD LogMessage
            vault.ObjectTypeOperations.RemoveObjectTypeAdmin(mfilesLogObjectTypeID);

            var mfilesLogMessagePD = vault.PropertyDefOperations.GetPropertyDefIDByAlias(structureConfig.LogMessagePropDefAlias);
            if (mfilesLogMessagePD != -1)
            {
                vault.PropertyDefOperations.RemovePropertyDefAdmin(mfilesLogMessagePD, DeleteFromClassesIfNecessary: true);
            }
        }


        /// <summary>
        /// Destroy all objects and vault structure for Log document class
        /// </summary>
        /// <param name="vault">vault to destroy in</param>
        /// <param name="structureConfig">configuration containing aliases of structure to remove</param>
        private static void DestroyAllLogFileObjectsAndVaultStructure(IVault vault, LoggingVaultStructureConfiguration structureConfig)
        {
            var mfilesLogFileClassID = vault.ClassOperations.GetObjectClassIDByAlias(structureConfig.LogFileClassAlias);
            if (mfilesLogFileClassID == -1) { return; }

            bool moreResults;
            do
            {
                var logFileObjectSearchResults  = SearchAllLogFileObjects(vault, mfilesLogFileClassID);
                moreResults                     = logFileObjectSearchResults.MoreResults;

                foreach(ObjectVersion logFileObjectVersion in logFileObjectSearchResults)
                {
                    vault.ObjectOperations.DestroyObject(logFileObjectVersion.ObjVer.ObjID, DestroyAllVersions:true, ObjectVersion:-1);
                }
            }
            while (moreResults);


            // CL LogFile
            vault.ClassOperations.RemoveObjectClassAdmin(mfilesLogFileClassID);
        }


        /// <summary>
        /// Search for the Log objects.
        /// </summary>
        /// <returns></returns>
        public static ObjectSearchResults SearchAllLogObjects(this IVault vault, int logObjectTypeID)
        {
            // Search for ObjectType "Log"
            var otSearchCondition = new SearchCondition();
            otSearchCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);
            otSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            otSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, logObjectTypeID);

            var searchConditions = new SearchConditions
            {
                { -1, otSearchCondition }
            };

            var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions, MFSearchFlags.MFSearchFlagNone, SortResults: false);

            // return 0, 1 or more Log objects for the current date, like "Log-2021-05-12", "Log-2021-05-12 (2)". They may not be sorted on title; we'll sort later on CreatedUtc

            return searchResults;
        }

        /// <summary>
        /// Search for the LogFile class (OT document) objects.
        /// </summary>
        /// <returns></returns>
        public static ObjectSearchResults SearchAllLogFileObjects(this IVault vault, int logFileClassID)
        {
            // Search for Class "LogFile"
            var otSearchCondition = new SearchCondition();
            otSearchCondition.Expression.DataPropertyValuePropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass;
            otSearchCondition.ConditionType = MFConditionType.MFConditionTypeEqual;
            otSearchCondition.TypedValue.SetValue(MFDataType.MFDatatypeLookup, logFileClassID);

            var searchConditions = new SearchConditions
            {
                { -1, otSearchCondition }
            };

            var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchConditions, MFSearchFlags.MFSearchFlagNone, SortResults: false);

            // return 0, 1 or more Log objects for the current date, like "Log-2021-05-12", "Log-2021-05-12 (2)". They may not be sorted on title; we'll sort later on CreatedUtc

            return searchResults;
        }
    }
}
