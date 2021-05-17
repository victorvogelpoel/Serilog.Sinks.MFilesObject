// VaultApplication.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Diagnostics;
using MFiles.VAF;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Core;
using MFilesAPI;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MFilesObject;

namespace DemoVaultApplication
{
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>
    public class VaultApplication
        : ConfigurableVaultApplicationBase<Configuration>
    {
        private readonly LoggingLevelSwitch  _loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);



        /// <summary>
        /// Sample event handler that logs check ins.
        /// </summary>
        /// <param name="env"></param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize, Class="MFiles.Class.Document")]
        public void BeforeCheckInChangesFinalizeUpdateLogDemo(EventHandlerEnvironment env)
        {
            Log.Information("User {User} has checked in document {DisplayID} at {TimeStamp}", env.CurrentUserID, env.DisplayID, DateTime.Now);
        }



        // ===========================================================================================================================================================
        // Logging configuration and settings

        /// <summary>
        /// Update the Serilog loggingLevelSwitch, if its configuration was changed in the Vault Administration.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientOps"></param>
        /// <param name="oldConfiguration"></param>
        protected override void OnConfigurationUpdated(IConfigurationRequestContext context, ClientOperations clientOps, Configuration oldConfiguration)
        {
            if (oldConfiguration.LogLevel != Configuration.LogLevel)
            {
                if (Configuration.LogLevel == "ERROR")        { _loggingLevelSwitch.MinimumLevel= LogEventLevel.Error;       }
                else if (Configuration.LogLevel == "WARNING") { _loggingLevelSwitch.MinimumLevel= LogEventLevel.Warning;     }
                else if (Configuration.LogLevel == "INFO")    { _loggingLevelSwitch.MinimumLevel= LogEventLevel.Information; }
                else if (Configuration.LogLevel == "None")    { _loggingLevelSwitch.MinimumLevel= LogEventLevel.Fatal;       }
            }

            Log.Information("Logging changed to {LogLevel}", Configuration.LogLevel);
        }


        /// <summary>
        /// Configure logging in the vault application, even create structure if necessary.
        /// </summary>
        /// <param name="vault"></param>
        public void ConfigureApplication(Vault vault)
        {
            // Configure logging
            // As this method is called from InitializeApplication, we can alter the vault structure, eg add ObjectType for Logging

            var structureConfig = new MFilesObjectLogSinkVaultStructureConfiguration
            {
                LogObjectTypeNameSingular   = "Log",
                LogObjectTypeNamePlural     = "Logs",
                LogMessagePropDefName       = "LogMessage",

                LogObjectTypeAlias          = "OT.Serilog.MFilesObjectLogSink.Log",
                LogClassAlias               = "CL.Serilog.MFilesObjectLogSink.Log",
                LogMessagePropDefAlias      = "PD.Serilog.MFilesObjectLogSink.LogMessage"
            };

            // Ensure that the structure for the logging-to-object is present in the vault, create if necessary.
            vault.EnsureLogSinkVaultStructure(structureConfig);

            // ------------------------------------------------------------------------------------------------------------------------------------
            // Build a Serilog logger with MFilesObjectLogSink.
            // Note to Log.CloseAndFlush() in the UninitializeApplication()!
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch)
                // Log to an 'rolling' object in the vault, eg objectType "Log" with a multiline text property.
                .WriteTo.MFilesObject(PermanentVault, mfilesLogObjectNamePrefix:     $"{ApplicationDefinition.Name}-Log-",
                                                      mfilesLogObjectTypeAlias:      structureConfig.LogObjectTypeAlias,
                                                      mfilesLogClassAlias:           structureConfig.LogClassAlias,
                                                      mfilesLogMessagePropDefAlias:  structureConfig.LogMessagePropDefAlias,
                                                      controlLevelSwitch:            _loggingLevelSwitch)
                .CreateLogger();

            Log.Information("VaultApplication {ApplicationName} has configured logging to an M-Files rolling Log object.", ApplicationDefinition.Name);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!
        }

        /// <summary>
        /// Initialize the Vault Application
        /// </summary>
        /// <param name="vault"></param>
        protected override void InitializeApplication(Vault vault)
        {
            base.InitializeApplication(vault);

            // Configure logging
            ConfigureApplication(vault);
        }

        /// <summary>
        /// Power down the vault application. At lease, flush the logging sinks.
        /// </summary>
        /// <param name="vault"></param>
        protected override void UninitializeApplication(Vault vault)
        {
            // IMPORTANT to flush any sink (like the batched MFilesObject sink).
            Log.CloseAndFlush();

            base.UninitializeApplication(vault);
        }

    }
}