// VaultApplication.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Diagnostics;
using System.Text;
using MFiles.VAF;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Core;
using MFilesAPI;
using Serilog;
using Serilog.Context;
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
        private StringBuilder _logEventBuffer = new StringBuilder();
        private Action _flushLogAction;
        private readonly MFilesObjectLogSinkVaultStructureConfiguration _loggingStructureConfig = new MFilesObjectLogSinkVaultStructureConfiguration
        {
            LogObjectTypeNameSingular   = "Log",
            LogObjectTypeNamePlural     = "Logs",
            LogMessagePropDefName       = "LogMessage",

            LogObjectTypeAlias          = "OT.Serilog.MFilesObjectLogSink.Log",
            LogClassAlias               = "CL.Serilog.MFilesObjectLogSink.Log",
            LogMessagePropDefAlias      = "PD.Serilog.MFilesObjectLogSink.LogMessage"
        };

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

            // Ensure that the structure for the logging-to-object is present in the vault, create if necessary.
            vault.EnsureLogSinkVaultStructure(_loggingStructureConfig);

#if DEBUG
            var sysUtilsEventLogLevel = LogEventLevel.Information;  // In DEBUG, we want to emit all messages to the event log, because we can access the Windows Event log on our dev machine.
#else
            var sysUtilsEventLogLevel = LogEventLevel.Error;        // In RELEASE, we want to emit only error messages to the event log; in a cloud vault, we cannot access the event log and we're only suppose to log ERRORS.

#endif

            // ------------------------------------------------------------------------------------------------------------------------------------
            // Build a Serilog logger with MFilesObjectLogSink.
            // Note to Log.CloseAndFlush() in the UninitializeApplication()!
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch)
                // Using a delegate to buffer log messages that are flushed later with a background job
                .WriteTo.DelegatingTextSink(w => WriteToVaultApplicationBuffer(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", levelSwitch:_loggingLevelSwitch)
                .WriteTo.MFilesSysUtilsEventLogSink(restrictedToMinimumLevel: sysUtilsEventLogLevel)   // Only write errors to the EventLog.
                .CreateLogger();


            // UNFORTUNATELY, the MFilesObjectlogSink CANNOT be created in a VaultApplication like below; WE DON'T CONTROL THE VAULT LIFECYCLE (as we do in the SANDBOX console application)
            // and it will invalidate soon after starting the vault application, yielding a "COM object that has been separated from its underlying RCW cannot be used."
            // when we should try and emit a LogEvent.
            // Hence using a DelegatingTextSink that collects the log messages and a background job that flushes the collected messages after 5 seconds.
            //
            //  .WriteTo.MFilesObject(vaultPersistent, mfilesLogObjectNamePrefix:     $"VaultApp-{ApplicationDefinition.Name}-Log-",
            //                                         mfilesLogObjectTypeAlias:      _loggingStructureConfig.LogObjectTypeAlias,
            //                                         mfilesLogClassAlias:           _loggingStructureConfig.LogClassAlias,
            //                                         mfilesLogMessagePropDefAlias:  _loggingStructureConfig.LogMessagePropDefAlias,
            //                                         controlLevelSwitch:            _loggingLevelSwitch)


            Log.Information("VaultApplication {ApplicationName} has configured logging to an M-Files rolling Log object.", ApplicationDefinition.Name);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!

            Log.Error("Sample error");
        }

        private void WriteToVaultApplicationBuffer(string formattedLogMessage)
        {
            _logEventBuffer.AppendLine(formattedLogMessage.TrimEnd(new char[]{ '\r', '\n'}));

            // Note that the backgroundoperation will flush these messages to a Log object, as the PermanentVault is a valid reference.
        }


        protected override void StartApplication()
        {
            // Define the delegate action for the flushing to the M-Files Object
            _flushLogAction = new Action(() =>
            {
                if (_logEventBuffer.Length > 0)
                {
                    var batchedLogMessage = _logEventBuffer.ToString();
                    _logEventBuffer.Clear();



                    var controlledSwitch    = new ControlledLevelSwitch(_loggingLevelSwitch);
                    var sink                = new MFilesObjectLogSink(this.PermanentVault, mfilesLogObjectNamePrefix: $"[{Environment.MachineName.ToUpperInvariant()}] VaultApp-{ApplicationDefinition.Name}-Log-",
                                                                            mfilesLogObjectTypeAlias:      _loggingStructureConfig.LogObjectTypeAlias,
                                                                            mfilesLogClassAlias:           _loggingStructureConfig.LogClassAlias,
                                                                            mfilesLogMessagePropDefAlias:  _loggingStructureConfig.LogMessagePropDefAlias,
                                                                            controlledSwitch:               controlledSwitch,
                                                                            formatProvider:                 null);
                    sink.EmitToMFilesLogObject(batchedLogMessage);
                }
            });

            this.BackgroundOperations.StartRecurringBackgroundOperation("Periodic Log-to-MFilesObject operation", TimeSpan.FromSeconds(5), _flushLogAction);

            base.StartApplication();
        }




        /// <summary>
        /// Initialize the Vault Application, including logging structure in the vault.
        /// </summary>
        /// <param name="vault"></param>
        protected override void InitializeApplication(Vault vault)
        {
            base.InitializeApplication(vault);

            // Configure logging
            ConfigureApplication(vault);
        }


        /// <summary>
        /// Power down the vault application. At least, flush the logging sinks.
        /// </summary>
        /// <param name="vault"></param>
        protected override void UninitializeApplication(Vault vault)
        {
            // IMPORTANT to flush any sink
            if (_flushLogAction != null) { _flushLogAction(); }
            Log.CloseAndFlush();

            base.UninitializeApplication(vault);
        }

    }
}