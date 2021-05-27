// Configuration.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.JsonAdaptor;

namespace DemoVaultApplication
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        [Security(ChangeBy = SecurityAttribute.UserLevel.SystemAdmin)]
        [JsonConfEditor(
            TypeEditor      = "options",
            Options         = "{selectOptions:[\"None\", \"ERROR\", \"WARNING\", \"INFO\"]}",
            Label           = "Log level",
            HelpText        = "Configure the level of logging messages to M-Files vault object",
            IsRequired      = false,
            DefaultValue    = "None")]
        public string LogLevel { get; set; } = "None";
    }
}