// VaultApplication.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Dramatic.LogToMFiles;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Configuration.Domain;
using MFiles.VAF.Configuration.Interfaces.Domain;
using MFiles.VAF.Core;
using MFilesAPI;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace DemoVaultApplication
{
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>
    public class VaultApplication
        : ConfigurableVaultApplicationBase<Configuration>
    {
        private static readonly object                                  _logBufferLockObj       = new object();
        private readonly LoggingLevelSwitch                             _loggingLevelSwitch     = new LoggingLevelSwitch(LogEventLevel.Information);
        private readonly string                                         _buildFileVersion       = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyFileVersionAttribute), false)).Version;

        private Action                                                  _flushLogAction;
        private readonly StringBuilder                                  _logEventBuffer         = new StringBuilder();
        private readonly MFilesObjectLoggingVaultStructureConfiguration _loggingStructureConfig = new MFilesObjectLoggingVaultStructureConfiguration
                                                                                                {
                                                                                                    LogObjectTypeNameSingular   = "Log",
                                                                                                    LogObjectTypeNamePlural     = "Logs",
                                                                                                    LogMessagePropDefName       = "LogMessage",

                                                                                                    LogObjectTypeAlias          = "OT.Serilog.MFilesObjectLogSink.Log",
                                                                                                    LogClassAlias               = "CL.Serilog.MFilesObjectLogSink.Log",
                                                                                                    LogMessagePropDefAlias      = "PD.Serilog.MFilesObjectLogSink.LogMessage"
                                                                                                };


        // ===========================================================================================================================================================
        // Logging configuration and settings

        /// <summary>
        /// Initialize the Vault Application, including logging structure in the vault.
        /// </summary>
        /// <param name="vault"></param>
        protected override void InitializeApplication(Vault vault)
        {
            base.InitializeApplication(vault);

            // Configure logging
            ConfigureLogging(Configuration.LogLevel);

            Log.Information("VaultApplication {ApplicationName} {BuildVersion} has been initialized", ApplicationDefinition.Name, _buildFileVersion);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!
        }


        ///// <summary>
        ///// Configure logging in the vault application, even create structure if necessary.
        ///// </summary>
        ///// <param name="vault"></param>
        //public void ConfigureApplication(Vault vault)
        //{
        //    // Configure logging
        //    // As this method is called from InitializeApplication, we can alter the vault structure, eg add ObjectType for Logging

        //    // Initialize the _loggingLevelSwitch from configuration
        //    ConfigureLoggingLevelSwitch(Configuration.LogLevel);

        //    // Ensure that the structure for the logging-to-object is present in the vault, create if necessary.
        //    vault.EnsureLogSinkVaultStructure(_loggingStructureConfig);

        //    // ------------------------------------------------------------------------------------------------------------------------------------
        //    // Build a Serilog logger with MFilesObjectLogSink.
        //    // Note to Log.CloseAndFlush() in the UninitializeApplication()!
        //    Log.Logger = new LoggerConfiguration()
        //        .Enrich.FromLogContext()
        //        .MinimumLevel.ControlledBy(_loggingLevelSwitch)

        //        // Using a delegate to buffer log messages that are flushed later with a background job
        //        .WriteTo.DelegatingTextSink(w => WriteToVaultApplicationBuffer(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

        //        .CreateLogger();


        //    // UNFORTUNATELY, the MFilesObjectlogSink CANNOT be created directly in a VaultApplication like below.
        //    // WE DON'T CONTROL THE VAULT REFERENCE LIFECYCLE (as we do in the SANDBOX console application) and it will
        //    // invalidate soon after starting the vault application, yielding a "COM object that has been separated from its underlying RCW cannot be used."
        //    // when we should try and emit a LogEvent.
        //    // Hence in a Vault Application, we use DelegatingTextSink that collects the log messages and a background job that flushes the collected messages after 5 seconds.
        //    //
        //    //  .WriteTo.MFilesObject(vaultPersistent, mfilesLogObjectNamePrefix:     $"VaultApp-{ApplicationDefinition.Name}-Log-",       // DO NOT USE in a Vault Application
        //    //                                         mfilesLogObjectTypeAlias:      _loggingStructureConfig.LogObjectTypeAlias,
        //    //                                         mfilesLogClassAlias:           _loggingStructureConfig.LogClassAlias,
        //    //                                         mfilesLogMessagePropDefAlias:  _loggingStructureConfig.LogMessagePropDefAlias,
        //    //                                         controlLevelSwitch:            _loggingLevelSwitch)


        //    Log.Information("VaultApplication {ApplicationName} has configured logging to an M-Files rolling Log object.", ApplicationDefinition.Name);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!
        //    Log.Warning("Sample warning");
        //    Log.Error("Sample error");
        //    Log.Error(new Exception("A sample exception"), "Sample error with exception");
        //}

        private void ConfigureLogging(string logLevelString)
        {
            _loggingLevelSwitch.MinimumLevel = GetLoggingLevelFor(logLevelString);

            // ------------------------------------------------------------------------------------------------------------------------------------
            // Build a Serilog logger with MFilesObjectLogSink.
            // Note to Log.CloseAndFlush() in the UninitializeApplication()!
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch)

                // Using a delegate to buffer log messages that are flushed later with a background job
                .WriteTo.DelegatingTextSink(w => WriteToVaultApplicationBuffer(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

                .CreateLogger();


            // UNFORTUNATELY, the MFilesObjectlogSink CANNOT be created directly in a vault application like below.
            // WE DON'T CONTROL THE VAULT REFERENCE LIFECYCLE (as we do in the SANDBOX console application) and it will invalidate
            // soon after starting the vault application, yielding a "COM object that has been separated from its underlying RCW cannot be used."
            // when we should try and emit a LogEvent.
            // Hence in a vault application, we use DelegatingTextSink that collects the log messages and a vault application background job that
            // flushes the collected messages after 5 seconds.
            //
            //  .WriteTo.MFilesObject(vaultPersistent, mfilesLogObjectNamePrefix:     $"VaultApp-{ApplicationDefinition.Name}-Log-",       // DO NOT USE in a Vault Application; you can use it where YOU control the vault reference, eq in a console application
            //                                         mfilesLogObjectTypeAlias:      _loggingStructureConfig.LogObjectTypeAlias,
            //                                         mfilesLogClassAlias:           _loggingStructureConfig.LogClassAlias,
            //                                         mfilesLogMessagePropDefAlias:  _loggingStructureConfig.LogMessagePropDefAlias,
            //                                         controlLevelSwitch:            _loggingLevelSwitch)
        }

        /// <summary>
        /// Calculate the Serilog logEventLevel from the vault application configured log level
        /// </summary>
        /// <param name="logLevelString"></param>
        /// <returns></returns>
        private LogEventLevel GetLoggingLevelFor(string logLevelString)
        {
            switch(logLevelString)
            {
                case "OFF":     return ((LogEventLevel) 1 + (int) LogEventLevel.Fatal);     // https://stackoverflow.com/questions/30849166/how-to-turn-off-serilog
                case "INFO":    return LogEventLevel.Information;
                case "WARNING": return LogEventLevel.Warning;
                case "ERROR":   return LogEventLevel.Error;
                default:        return LogEventLevel.Information;
            }
        }

        /// <summary>
        /// When the admin changes the log event in the configuration of the vault application, set the corresponding Serilog LogEventLevel
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientOps"></param>
        /// <param name="oldConfiguration"></param>
        protected override void OnConfigurationUpdated(IConfigurationRequestContext context, ClientOperations clientOps, Configuration oldConfiguration)
        {
            if (oldConfiguration.LogLevel != Configuration.LogLevel)
            {
                _loggingLevelSwitch.MinimumLevel = GetLoggingLevelFor(Configuration.LogLevel);
            }

            Log.Information("Admin changed Log level to {LogLevel}", Configuration.LogLevel);
        }


        /// <summary>
        /// Buffer log events. Note that the backgroundoperation will flush these messages to a Log object later, and will use the PermanentVault is a valid vault reference.
        /// </summary>
        /// <param name="formattedLogMessage"></param>
        private void WriteToVaultApplicationBuffer(string formattedLogMessage)
        {
            lock(_logBufferLockObj)
            {
                _logEventBuffer.AppendLine(formattedLogMessage.TrimEnd(Environment.NewLine.ToCharArray()));
            }
        }


        /// <summary>
        /// Start the Vault Application
        /// </summary>
        protected override void StartApplication()
        {
            // Define the delegate action for the flushing to the M-Files Object
            _flushLogAction = new Action(() =>
            {
                if (_logEventBuffer.Length > 0)
                {
                    string batchedLogMessages;

                    lock(_logBufferLockObj)
                    {
                        batchedLogMessages = _logEventBuffer.ToString();
                        _logEventBuffer.Clear();
                    }

                    var repository = new MFilesLogObjectRepository(PermanentVault,
                                                                   mfilesLogObjectNamePrefix:     $"[{Environment.MachineName.ToUpperInvariant()}] DemoVaultApp-Log-",
                                                                   mfilesLogObjectTypeAlias:      _loggingStructureConfig.LogObjectTypeAlias,
                                                                   mfilesLogClassAlias:           _loggingStructureConfig.LogClassAlias,
                                                                   mfilesLogMessagePropDefAlias:  _loggingStructureConfig.LogMessagePropDefAlias);

                    repository.WriteLogMessage(batchedLogMessages);
                }
            });

#pragma warning disable CS0618 // Type or member is obsolete
            this.BackgroundOperations.StartRecurringBackgroundOperation("Periodic Log-to-MFilesObject operation", TimeSpan.FromSeconds(5), _flushLogAction);
#pragma warning restore CS0618 // Type or member is obsolete

            base.StartApplication();
        }


        /// <summary>
        /// Power down the vault application. At least, flush the logging sinks.
        /// </summary>
        /// <param name="vault"></param>
        protected override void UninitializeApplication(Vault vault)
        {
            Log.Information("VaultApplication {ApplicationName} {BuildVersion} is powering down.", ApplicationDefinition.Name, _buildFileVersion);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!

            // IMPORTANT: flush any buffered messages
            _flushLogAction?.Invoke();

            // IMPORTANT: flush all sinks
            Log.CloseAndFlush();

            base.UninitializeApplication(vault);
        }


        [VaultExtensionMethod("SampleVaultApp.Serilog.Sinks.MFilesObject.EnsureLoggingVaultStructure", RequiredVaultAccess = MFVaultAccess.MFVaultAccessChangeFullControlRole)]  // MFVaultAccess.MFVaultAccessChangeFullControlRole  / MFVaultAccess.MFVaultAccessChangeMetaDataStructure
        private string EnsureLoggingVaultStructure(EventHandlerEnvironment env)
        {
            PermanentVault.EnsureLoggingVaultStructure(_loggingStructureConfig);

            // Reinitialize the PermanentVault cache, otherwise the Logging structure changes won't be noticed.
            ReinitializeMetadataStructureCache(PermanentVault);

            return "Logging structure has been created in the vault";
        }

        [VaultExtensionMethod("SampleVaultApp.Serilog.Sinks.MFilesObject.RemoveLoggingVaultStructure", RequiredVaultAccess = MFVaultAccess.MFVaultAccessChangeFullControlRole)]  // MFVaultAccess.MFVaultAccessChangeFullControlRole  / MFVaultAccess.MFVaultAccessChangeMetaDataStructure
        private string RemoveLoggingVaultStructure(EventHandlerEnvironment env)
        {
            PermanentVault.RemoveLogObjectsAndLoggingVaultStructure(_loggingStructureConfig);

            // Reinitialize the PermanentVault cache, otherwise the Logging structure changes won't be noticed.
            ReinitializeMetadataStructureCache(PermanentVault);

            return "Logging objects and logging vault structure are removed from the vault";
        }


        // TODO: log method to be used from VBScript:
        [VaultExtensionMethod("SampleVaultApp.LogInformation")] // RequiredVaultAccess = MFVaultAccess.MFVaultAccessSeeAllDocs | MFVaultAccess.MFVaultAccessCreateDocs | MFVaultAccess.MFVaultAccessEditAllDocs | MFVaultAccess.MFVaultAccessForceUndoCheckout
        private string LogInformation(EventHandlerEnvironment env)
        {
            Log.Information(env.Input);
            return $"Logged the following Information message:\r\n{env.Input}";
        }

        [VaultExtensionMethod("SampleVaultApp.LogWarning")] // RequiredVaultAccess = MFVaultAccess.MFVaultAccessSeeAllDocs | MFVaultAccess.MFVaultAccessCreateDocs | MFVaultAccess.MFVaultAccessEditAllDocs | MFVaultAccess.MFVaultAccessForceUndoCheckout
        private string LogWarning(EventHandlerEnvironment env)
        {
            Log.Warning(env.Input);
            return $"Logged the following warning message:\r\n{env.Input}";
        }

        [VaultExtensionMethod("SampleVaultApp.LogError")] // RequiredVaultAccess = MFVaultAccess.MFVaultAccessSeeAllDocs | MFVaultAccess.MFVaultAccessCreateDocs | MFVaultAccess.MFVaultAccessEditAllDocs | MFVaultAccess.MFVaultAccessForceUndoCheckout
        private string LogError(EventHandlerEnvironment env)
        {
            Log.Error(env.Input);
            return $"Logged the following error message:\r\n{env.Input}";
        }


        private readonly CustomDomainCommand cmdEnsureLoggingVaultStructureMenuItem = new CustomDomainCommand
        {
            ID              = "cmdEnsureLoggingVaultStructure",
            ConfirmMessage  = "Are you sure you want to add logging vault structure?",
            Execute         = (context, operations) => operations.ShowMessage(context.Vault.ExtensionMethodOperations.ExecuteVaultExtensionMethod("SampleVaultApp.Serilog.Sinks.MFilesObject.EnsureLoggingVaultStructure", "")),
            DisplayName     = "Logging: Add logging vault structure",
            Locations       = new List<ICommandLocation> { new DomainMenuCommandLocation(icon: "plus") }
        };

        private readonly CustomDomainCommand cmdRemoveLoggingVaultStructureMenuItem = new CustomDomainCommand
        {
            ID              = "cmdRemoveLoggingVaultStructure",
            ConfirmMessage  = "Are you sure you want to remove logging objects and logging vault structure?",
            Execute         = (context, operations) => operations.ShowMessage(context.Vault.ExtensionMethodOperations.ExecuteVaultExtensionMethod("SampleVaultApp.Serilog.Sinks.MFilesObject.RemoveLoggingVaultStructure", "")),
            DisplayName     = "Logging: remove logging vault structure",
            Locations       = new List<ICommandLocation> { new DomainMenuCommandLocation(icon: "trash") }
        };

        private readonly CustomDomainCommand cmdTestLogMessageMenuItem = new CustomDomainCommand
        {
            ID              = "cmdTestLogMessage",
            Execute         = (context, operations) => operations.ShowMessage(context.Vault.ExtensionMethodOperations.ExecuteVaultExtensionMethod("SampleVaultApp.LogInformation", $"[{DateTime.Now:HH:mm:ss} INF] Testing, one, two, three. Logged with love from the vault application domain area.")),
            DisplayName     = "Logging: log a test message",
            Locations       = new List<ICommandLocation> { new DomainMenuCommandLocation(icon: "play") }
        };

        public override IEnumerable<CustomDomainCommand> GetCommands(IConfigurationRequestContext context)
        {
	        return new List<CustomDomainCommand>(base.GetCommands(context))
	        {
		        cmdEnsureLoggingVaultStructureMenuItem,
                cmdRemoveLoggingVaultStructureMenuItem,
                cmdTestLogMessageMenuItem
	        };
        }

        // ===========================================================================================================================================================
        // The business use case events

        /// <summary>
        /// Sample event handler that logs check ins.
        /// </summary>
        /// <param name="env"></param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize, ObjectType = (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument)]
        public void BeforeCheckInChangesFinalizeUpdateLogDemo(EventHandlerEnvironment env)
        {
            using (LogContext.PushProperty("MFEventType", env.EventType.ToString()))  // Note "Enrich.FromLogContext()" in the configuration builder!
            {
                // Now every log event in this scope automatically has this additional property "MFEventType" from the M-Files event handler environment!

                Log.Information("User {User} has checked in document {DisplayID} at {TimeStamp}", env.CurrentUserID, env.DisplayID, DateTime.Now);

                // ... do other stuff
            }
        }
    }
}