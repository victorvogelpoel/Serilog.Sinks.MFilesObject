// MFilesObjectLogSinkVaultStructure.cs
// 27-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using MFilesAPI;

namespace Dramatic.LogToMFiles
{
    public class MFilesObjectLoggingVaultStructureConfiguration
    {
        public string LogObjectTypeNameSingular { get; set; } = MFilesObjectLoggingVaultStructure.DefaultLogObjectTypeNameSingular;
        public string LogObjectTypeNamePlural   { get; set; } = MFilesObjectLoggingVaultStructure.DefaultLogObjectTypeNamePlural;
        public string LogMessagePropDefName     { get; set; } = MFilesObjectLoggingVaultStructure.DefaultLogMessagePropDefName;

        public string LogObjectTypeAlias        { get; set; } = MFilesObjectLoggingVaultStructure.DefaultMFilesLogObjectTypeAlias;
        public string LogClassAlias             { get; set; } = MFilesObjectLoggingVaultStructure.DefaultMFilesLogClassAlias;
        public string LogMessagePropDefAlias    { get; set; } = MFilesObjectLoggingVaultStructure.DefaultMFilesLogMessagePropertyDefinitionAlias;

        public string LogFileClassName          { get; set; } = MFilesObjectLoggingVaultStructure.DefaultLogFileClassName;
        public string LogFileClassAlias         { get; set; } = MFilesObjectLoggingVaultStructure.DefaultMFilesLogFileClassAlias;
    }


    public static class MFilesObjectLoggingVaultStructure
    {
        public const string DefaultLogObjectTypeNameSingular                = "Log";
        public const string DefaultLogObjectTypeNamePlural                  = "Logs";
        public const string DefaultLogMessagePropDefName                    = "LogMessage";

        public const string DefaultMFilesLogObjectTypeAlias                 = "OT.Serilog.MFilesObjectLogSink.Log";
        public const string DefaultMFilesLogClassAlias                      = "CL.Serilog.MFilesObjectLogSink.Log";
        public const string DefaultMFilesLogMessagePropertyDefinitionAlias  = "PD.Serilog.MFilesObjectLogSink.LogMessage";
        public const string DefaultMFilesLogFileClassAlias                  = "CL.Serilog.MFilesObjectLogSink.LogFile";
        public const string DefaultMFilesLogObjectNamePrefix                = "Log-";

        public const string DefaultLogFileClassName                         = "LogFile";
        public const string DefaultMFilesLogFileNamePrefix                  = "Log-";


        public static object StructureChangeLock = new Object();


        /// <summary>
        /// Test if the logging structure is present in the vault
        /// </summary>
        /// <param name="vault"></param>
        /// <param name="structureConfig"></param>
        /// <returns></returns>
        public static bool HasLoggingVaultStructure(this IVault vault, MFilesObjectLoggingVaultStructureConfiguration structureConfig)
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
        public static void EnsureLoggingVaultStructure(this IVault vault, MFilesObjectLoggingVaultStructureConfiguration structureConfig)
        {
            // Add OT "Log" with CL "Log"
            // Add PD "LogMessage"

            if (vault is null)                                          { throw new ArgumentNullException(nameof(vault)); }
            if (vault.GetType().FullName == "MetadataCacheVault_Dyn")   { throw new InvalidOperationException($"The PermanentVault reference cannot be used. It is a vault with cached structure and will yield structural lookup errors when creating the logging structure."); }
            if (structureConfig is null)                                { throw new ArgumentNullException(nameof(structureConfig)); }

            lock(StructureChangeLock)
            {
                var logObjectTypeID     = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(structureConfig.LogObjectTypeAlias);
                var logMessagePropDefID = vault.PropertyDefOperations.GetPropertyDefIDByAlias(structureConfig.LogMessagePropDefAlias);
                var logFileClassID      = vault.ClassOperations.GetObjectClassIDByAlias(structureConfig.LogFileClassAlias);


                // Add Log ObjectType if it doesn't exist
                if (logObjectTypeID == -1)
                {
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
                        AccessControlList               = new AccessControlList(),
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
        public static void RemoveLogObjectsAndLoggingVaultStructure(this IVault vault, MFilesObjectLoggingVaultStructureConfiguration structureConfig)
        {
            if (vault is null)                                          { throw new ArgumentNullException(nameof(vault)); }
            if (vault.GetType().FullName == "MetadataCacheVault_Dyn")   { throw new InvalidOperationException($"The PermanentVault reference cannot be used. It is a vault with cached structure and will yield structural lookup errors when creating the logging structure."); }

            lock(StructureChangeLock)
            {
                DestroyAllLogObjectsAndVaultStructure(vault, structureConfig);

                DestroyAllLogFileObjectsAndVaultStructure(vault, structureConfig);
            }
        }

        private static void DestroyAllLogObjectsAndVaultStructure(IVault vault, MFilesObjectLoggingVaultStructureConfiguration structureConfig)
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


        private static void DestroyAllLogFileObjectsAndVaultStructure(IVault vault, MFilesObjectLoggingVaultStructureConfiguration structureConfig)
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
        private static ObjectSearchResults SearchAllLogObjects(IVault vault, int logObjectTypeID)
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
        private static ObjectSearchResults SearchAllLogFileObjects(IVault vault, int logFileClassID)
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
