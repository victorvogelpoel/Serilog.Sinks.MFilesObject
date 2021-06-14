// VaultApplication.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Dramatic.LogToMFiles;
using MFiles.VAF;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Core;
using MFilesAPI;
using Serilog;
using Serilog.Context;
using Serilog.Core;
//using Serilog.Destructuring;
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
        private static readonly object                                  _logBufferLockObj       = new object();
        private readonly LoggingLevelSwitch                             _loggingLevelSwitch     = new LoggingLevelSwitch(LogEventLevel.Information);
        private readonly string                                         _buildFileVersion       = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyFileVersionAttribute), false)).Version;

        private Action                                                  _flushLogAction;
        private readonly StringBuilder                                  _logEventBuffer         = new StringBuilder();
        private readonly MFilesObjectLogSinkVaultStructureConfiguration _loggingStructureConfig = new MFilesObjectLogSinkVaultStructureConfiguration
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
            ConfigureApplication(vault);
        }


        /// <summary>
        /// Configure logging in the vault application, even create structure if necessary.
        /// </summary>
        /// <param name="vault"></param>
        public void ConfigureApplication(Vault vault)
        {
            // Configure logging
            // As this method is called from InitializeApplication, we can alter the vault structure, eg add ObjectType for Logging

            // Initialize the _loggingLevelSwitch from configuration
            ConfigureLoggingLevelSwitch(Configuration.LogLevel);

            // Ensure that the structure for the logging-to-object is present in the vault, create if necessary.
            vault.EnsureLogSinkVaultStructure(_loggingStructureConfig);

            // ------------------------------------------------------------------------------------------------------------------------------------
            // Build a Serilog logger with MFilesObjectLogSink.
            // Note to Log.CloseAndFlush() in the UninitializeApplication()!
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch)

                // Using a delegate to buffer log messages that are flushed later with a background job
                .WriteTo.DelegatingTextSink(w => WriteToVaultApplicationBuffer(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

                .CreateLogger();


            // UNFORTUNATELY, the MFilesObjectlogSink CANNOT be created directly in a VaultApplication like below.
            // WE DON'T CONTROL THE VAULT LIFECYCLE (as we do in the SANDBOX console application) and it will
            // invalidate soon after starting the vault application, yielding a "COM object that has been separated from its underlying RCW cannot be used."
            // when we should try and emit a LogEvent.
            //
            // Hence using a DelegatingTextSink that collects the log messages and a background job that flushes the collected messages after 5 seconds.
            //
            //  .WriteTo.MFilesObject(vaultPersistent, mfilesLogObjectNamePrefix:     $"VaultApp-{ApplicationDefinition.Name}-Log-",
            //                                         mfilesLogObjectTypeAlias:      _loggingStructureConfig.LogObjectTypeAlias,
            //                                         mfilesLogClassAlias:           _loggingStructureConfig.LogClassAlias,
            //                                         mfilesLogMessagePropDefAlias:  _loggingStructureConfig.LogMessagePropDefAlias,
            //                                         controlLevelSwitch:            _loggingLevelSwitch)


            Log.Information("VaultApplication {ApplicationName} has configured logging to an M-Files rolling Log object.", ApplicationDefinition.Name);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!
            Log.Warning("Sample warning");
            Log.Error("Sample error");
            Log.Error(new Exception("A sample exception"), "Sample error with exception");
        }

        private void ConfigureLoggingLevelSwitch(string logLevel)
        {
            switch(logLevel)
            {
                case "OFF":     _loggingLevelSwitch.MinimumLevel = ((LogEventLevel) 1 + (int) LogEventLevel.Fatal);     break;  // https://stackoverflow.com/questions/30849166/how-to-turn-off-serilog
                case "INFO":    _loggingLevelSwitch.MinimumLevel = LogEventLevel.Information;                           break;
                case "WARNING": _loggingLevelSwitch.MinimumLevel = LogEventLevel.Warning;                               break;
                case "ERROR":   _loggingLevelSwitch.MinimumLevel = LogEventLevel.Error;                                 break;
                default:        _loggingLevelSwitch.MinimumLevel = LogEventLevel.Information;                           break;
            }
        }

        /// <summary>
        /// Update the Serilog loggingLevelSwitch, when the LogLevel configuration for the Vault Application is changed in M-Files Admin.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientOps"></param>
        /// <param name="oldConfiguration"></param>
        protected override void OnConfigurationUpdated(IConfigurationRequestContext context, ClientOperations clientOps, Configuration oldConfiguration)
        {
            if (oldConfiguration.LogLevel != Configuration.LogLevel)
            {
                ConfigureLoggingLevelSwitch(Configuration.LogLevel);
            }

            Log.Information("Log level changed to {LogLevel}", Configuration.LogLevel);
        }


        private void WriteToVaultApplicationBuffer(string formattedLogMessage)
        {
            lock(_logBufferLockObj)
            {
                _logEventBuffer.AppendLine(formattedLogMessage.TrimEnd(Environment.NewLine.ToCharArray()));
            }

            // Note that the backgroundoperation will flush these messages to a Log object later, and will use the PermanentVault is a valid vault reference.
        }


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

                    var repository = new MFilesLogObjectRepository(this.PermanentVault,
                                                             mfilesLogObjectNamePrefix:     $"[{Environment.MachineName.ToUpperInvariant()}] VaultApp-{ApplicationDefinition.Name}-Log-",
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
            // IMPORTANT: flush any buffered messages
            _flushLogAction?.Invoke();

            // IMPORTANT: flush all sinks
            Log.CloseAndFlush();

            base.UninitializeApplication(vault);
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

                // ... do stuff
            }
        }
    }
}