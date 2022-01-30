// AssemblyDebuggerDisplay.cs
// 19-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System.Diagnostics;
using MFilesAPI;

[assembly: DebuggerDisplay("AssociatedPropertyDef {PropertyDef} {Required ? \"Required\": \"\"}", Target = typeof(AssociatedPropertyDefClass))]
[assembly: DebuggerDisplay("ClassGroup {Name}, {Classes.Count} classes", Target = typeof(ClassGroupClass))]
[assembly: DebuggerDisplay("Lookup {DisplayValue}, item {Item}, version {Version} (hidden {Hidden}, deleted {Deleted})", Target = typeof(LookupClass))]
[assembly: DebuggerDisplay("ObjectCreationInfo {PropertyValues.Length}, PropertyValues, {Files.Length} Files", Target = typeof(ObjectCreationInfoClass))]
[assembly: DebuggerDisplay("ObjectClass {Name} ({ID}), ObjType={ObjType}", Target = typeof(ObjectClassClass))]
[assembly: DebuggerDisplay("ObjectVersion {Title} {DisplayID}, class {Class}, {Files.Count} Files, deleted={Deleted}", Target = typeof(ObjectVersionClass))]
[assembly: DebuggerDisplay("ObjectFile {Name} ({ID} {Version}), {Extension}, size {Size}", Target = typeof(ObjectFileClass))]
[assembly: DebuggerDisplay("ObjID {ID}, {Type}", Target = typeof(ObjIDClass))]
[assembly: DebuggerDisplay("ObjVer {ID} {Version}, type {Type}", Target = typeof(ObjVerClass))]
[assembly: DebuggerDisplay("ObjType {NamePlural} ({ID}) ", Target = typeof(ObjTypeClass))]
[assembly: DebuggerDisplay("OwnerPropertyDef {ID}", Target = typeof(OwnerPropertyDefClass))]
[assembly: DebuggerDisplay("PropertyDef {Name} ({ID}) {DataType}, OT {ObjectType}, VL {ValueList}", Target = typeof(PropertyDefClass))]
[assembly: DebuggerDisplay("PropertyValue def {PropertyDef}, value {TypedValue}", Target = typeof(PropertyValueClass))]
[assembly: DebuggerDisplay("TypedValue {DataType} {DisplayValue}", Target = typeof(TypedValueClass))]
[assembly: DebuggerDisplay("ValueListItem {Name} ({DisplayID}), VL {ValueListID}", Target = typeof(ValueListItemClass))]
[assembly: DebuggerDisplay("Vault {Name} {GUID}", Target = typeof(VaultClass))]
[assembly: DebuggerDisplay("Workflow {Name} ({ID}), objectclass {ObjectClass}", Target = typeof(WorkflowClass))]

//[assembly: DebuggerDisplay("AutomaticMetadataRequestInfo ObjectType {ObjectType}, ObjVer {ObjVer}, {UploadIds.Count} uploadIds, {PropertyValues.Count} PropertyValues, {MetadataProviderIds.Count} MetadataProviderIds", Target = typeof(AutomaticMetadataRequestInfo))]
//[assembly: DebuggerDisplay("Authentication {Url}, {VaultGuid}, {UserName}", Target = typeof(Authentication))]
//[assembly: DebuggerDisplay("Permissions CanRead {CanRead}, CanEdit {CanEdit}, CanAttachObjects {CanAttachObjects}", Target = typeof(Permissions))]
//[assembly: DebuggerDisplay("UploadInfo {Title} ({UploadID}), ext {Extension}, size {Size}", Target = typeof(UploadInfo))]
//[assembly: DebuggerDisplay("ExtendedObjectClass {Name} ({ID}), ObjType={ObjType}", Target = typeof(ExtendedObjectClass))]
//[assembly: DebuggerDisplay("ExtendedObjectVersion {Title} {DisplayID}, class {Class}, {Files.Count} Files, {Properties.Count}, Properties {PropertiesForDisplay.Count}, PropertiesForDisplay, {AddedFiles.Count} AddedFiles", Target = typeof(ExtendedObjectVersion))]

// [assembly: DebuggerDisplay("", Target = typeof(SomeType))]
// [assembly: DebuggerDisplay("", Target = typeof(SomeType))]
// [assembly: DebuggerDisplay("", Target = typeof(SomeType))]
