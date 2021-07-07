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


        public static object ChangeLock = new Object();

        /// <summary>
        /// Ensure the structure in the M-Files vault needed for logging; full control permissions are necessary to create or update structure.
        /// </summary>
        /// <param name="vault">Reference to the M-Files vault; make sure you're connected to the vault with full control permissions.</param>
        /// <param name="structureConfig">Settings for creating Log structure in the vault.</param>
        public static void EnsureLogSinkVaultStructure(this IVault vault, MFilesObjectLoggingVaultStructureConfiguration structureConfig)
        {
            // Add OT "Log" with CL "Log"
            // Add PD "LogMessage"

            lock(ChangeLock)
            {

                if (vault == null) throw new ArgumentNullException(nameof(vault));

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
                if (classesForLogObjectType.Count != 1) { throw new InvalidOperationException($"OT {structureConfig.LogObjectTypeAlias} should have only 1 class associated, found {classesForLogObjectType.Count}."); }

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



        public static void RemoveLogObjectsAndLoggingVaultStructure(this IVault vault, MFilesObjectLoggingVaultStructureConfiguration structureConfig)
        {
            lock(ChangeLock)
            {
                //var logObjectSearchResults = SearchAllLogObjects();


                // Delete all objects for the objecttype/class
                // Then remove the structure
            }
        }

}
