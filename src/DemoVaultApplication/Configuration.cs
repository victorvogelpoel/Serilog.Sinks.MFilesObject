// Configuration.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System.Runtime.Serialization;
using Dramatic.LogToMFiles;
using MFiles.VAF.Configuration;

namespace DemoVaultApplication
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        public LoggingConfiguration LoggingConfiguration { get; set; }
    }


    [DataContract]
    public class LoggingConfiguration
    {
        [DataMember]
        [Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin)]
        [JsonConfEditor(
            TypeEditor      = "options",
            IsRequired      = true,
            Options         = "{selectOptions:[\"OFF\", \"INFO\", \"WARNING\", \"ERROR\"]}",
            DefaultValue    = "OFF",
            Label           = "Log level",
            HelpText        = "Configure the minimal log level of writing events to the M-Files vault object"
            )]
        public string LogLevel { get; set; } = "OFF";


        [MFObjType(Required = true)]
        [DataMember]
        [Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin)]
        [JsonConfEditor(
            Label           = "Log Object type",
            IsRequired      = true,
            DefaultValue    = MFilesObjectLoggingVaultStructure.DefaultMFilesLogObjectTypeAlias,
            Hidden          = true, ShowWhen = ".parent._children{.key == 'LogLevel' && .value != 'OFF' }")]
        public MFIdentifier LogOT { get; set; } =  MFilesObjectLoggingVaultStructure.DefaultMFilesLogObjectTypeAlias;


        [MFClass(Required = true, RefMember = nameof(LoggingConfiguration.LogOT))]
        [DataMember]
        [Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin)]
        [JsonConfEditor(
            Label           = "Log class",
            IsRequired      = true,
            DefaultValue    = MFilesObjectLoggingVaultStructure.DefaultMFilesLogClassAlias,
            Hidden          = true,
            ShowWhen        = ".parent._children{.key == 'LogLevel' && .value != 'OFF' }")]
        public MFIdentifier LogCL { get; set; } = MFilesObjectLoggingVaultStructure.DefaultMFilesLogClassAlias;


        [MFPropertyDef(Required = true, RefMember = nameof(LoggingConfiguration.LogCL))]
        [DataMember]
        [Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin)]
        [JsonConfEditor(
            Label           = "LogMessage MultilineText property",
            IsRequired      = true,
            DefaultValue    = MFilesObjectLoggingVaultStructure.DefaultMFilesLogMessagePropertyDefinitionAlias,
            Hidden = true, ShowWhen = ".parent._children{.key == 'LogLevel' && .value != 'OFF' }")]
        public MFIdentifier LogMessagePD { get; set; } = MFilesObjectLoggingVaultStructure.DefaultMFilesLogMessagePropertyDefinitionAlias;


        [DataMember]
        [Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin)]
        [JsonConfEditor(
            Label           = "LogObject NameOrTitle prefix",
            IsRequired      = true,
            DefaultValue    = "DemoVaultApp-Log-",
            Hidden = true, ShowWhen = ".parent._children{.key == 'LogLevel' && .value != 'OFF' }")]
        public string LogObjectNamePrefix { get; set; } = "DemoVaultApp-Log-";


        //[MFClass(Required = true)]
        //[DataMember]
        //[Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin)]
        //[JsonConfEditor(Label = "Log File class", IsRequired = true, DefaultValue = MFilesObjectLoggingVaultStructure.DefaultMFilesLogFileClassAlias, Hidden = true, ShowWhen = ".parent._children{.key == 'LogLevel' && .value != 'OFF' }")]
        //public MFIdentifier LogFileCL { get; set; } = MFilesObjectLoggingVaultStructure.DefaultMFilesLogFileClassAlias;




    }
}