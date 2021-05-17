// MFilesObjectLogSinkVaultStructure.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using MFilesAPI;

namespace Serilog.Sinks.MFilesObject
{
    public class MFilesObjectLogSinkVaultStructureConfiguration
    {
        public string LogObjectTypeNameSingular { get; set; } = MFilesObjectLogSinkVaultStructure.DefaultLogObjectTypeNameSingular;
        public string LogObjectTypeNamePlural   { get; set; } = MFilesObjectLogSinkVaultStructure.DefaultLogObjectTypeNamePlural;
        public string LogMessagePropDefName     { get; set; } = MFilesObjectLogSinkVaultStructure.DefaultLogMessagePropDefName;

        public string LogObjectTypeAlias        { get; set; } = MFilesObjectLogSink.DefaultMFilesLogObjectTypeAlias;
        public string LogClassAlias             { get; set; } = MFilesObjectLogSink.DefaultMFilesLogClassAlias;
        public string LogMessagePropDefAlias    { get; set; } = MFilesObjectLogSink.DefaultMFilesLogMessagePropertyDefinitionAlias;
    }


    public static class MFilesObjectLogSinkVaultStructure
    {
        public const string DefaultLogObjectTypeNameSingular    = "Log";
        public const string DefaultLogObjectTypeNamePlural      = "Logs";
        public const string DefaultLogMessagePropDefName        = "LogMessage";

        ///// <summary>
        ///// Ensure that the ObjectType, Class and PropertyDefinitions are present in the vault, or create these
        ///// </summary>
        ///// <remarks>This works, although M-Files CloudOps teams is never gonna allow updating the vault structure</remarks>
        ///// <param name="vault"></param>
        //public static void Ensure(IVault vault)
        //{
        //    var logMessagePropDefId = vault.PropertyDefOperations.GetPropertyDefIDByAlias(DefaultMFilesLogMessagePropertyDefinitionAlias);
        //    var logObjectTypeId     = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(DefaultMFilesLogObjectTypeAlias);
        //    var logClassId          = vault.ClassOperations.GetObjectClassIDByAlias(DefaultMFilesLogClassAlias);

        //    // If vault structure for Log-to-MFilesObject is not present, then add it via content import
        //    if (logMessagePropDefId == -1 || logObjectTypeId == -1 || logClassId == -1)
        //    {
        //        var importContentJob = new ImportContentJob()
        //        {
        //            SourceLocation = @"C:\PROJECTS\GeONE.LAB.MFilesVaultEventTracer\src\Serilog.Sinks.MFilesObject\assets\{B48A27F9-F7FC-4641-A405-DE7FABA85EE2}_20210511_215545\index.xml"
        //        };

        //        vault.ManagementOperations.ImportContent(importContentJob);
        //    }
        //}


        public static void EnsureLogSinkVaultStructure(this IVault vault, MFilesObjectLogSinkVaultStructureConfiguration structureConfig)
        {
            if (vault == null) throw new ArgumentNullException(nameof(vault));

            var logObjectTypeID     = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(structureConfig.LogObjectTypeAlias);
            var logMessagePropDefID = vault.PropertyDefOperations.GetPropertyDefIDByAlias(structureConfig.LogMessagePropDefAlias);


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
                    Translatable                    = false
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

            // Now get the associated classes for the objecttype (should be 1)
            var classesForLogObjectType = vault.ClassOperations.GetObjectClassesAdmin(logObjectTypeID);
            if (classesForLogObjectType.Count != 1) { throw new InvalidOperationException($"OT {structureConfig.LogObjectTypeAlias} should have only 1 class associated, found {classesForLogObjectType.Count}."); }

            var classForLogObjectType   = classesForLogObjectType[1];
            var classAliases            = ";" + classForLogObjectType.SemanticAliases.Value.Trim() + ";";               // ";;", ";CL.SomeAlias;" or ";OT.Serilog.MFilesObjectLogSink.Log;"
            var classHasBeenUpdated     = classAliases.Contains(";" + structureConfig.LogClassAlias.Trim() + ";");      // test if ";OT.Serilog.MFilesObjectLogSink.Log; is part of the alias => class Log was already configured.

            if (!classHasBeenUpdated)
            {
                var newAlias = (classForLogObjectType.SemanticAliases.Value.Trim(new char[] {' ', ';' }) + ";" + structureConfig.LogClassAlias.Trim()).Trim(';');
                // Class for object type has not been updated yet, so add LogMessage PD and alias

                var associatedLogMessagePropDef = new AssociatedPropertyDef
                {
                    PropertyDef                     = logMessagePropDefID,
                    Required                        = false
                };

                classForLogObjectType.AssociatedPropertyDefs.Add(-1, associatedLogMessagePropDef);

                // Update Alias, adding if another exists
                classForLogObjectType.SemanticAliases.Value = (classForLogObjectType.SemanticAliases.Value.Trim(new char[] {' ', ';' }) + ";" + structureConfig.LogClassAlias.Trim()).Trim(';');

                vault.ClassOperations.UpdateObjectClassAdmin(classForLogObjectType);
            }
        }



        public static void Remove(IVault vault, string mfilesLogObjectTypeAlias = MFilesObjectLogSink.DefaultMFilesLogObjectTypeAlias, string mfilesLogClassAlias = MFilesObjectLogSink.DefaultMFilesLogClassAlias, string mfilesLogMessagePropDefAlias = MFilesObjectLogSink.DefaultMFilesLogMessagePropertyDefinitionAlias)
        {
            // TODO: Delete all objects for the objecttype/class
            // Then remove the structure


        }
    }
}
