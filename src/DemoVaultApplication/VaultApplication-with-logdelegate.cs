// VaultApplication.cs
// 3-9-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Dramatic.LogToMFiles;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Configuration.Domain;
using MFiles.VAF.Configuration.Domain.Dashboards;
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
    public class VaultApplication : ConfigurableVaultApplicationBase<Configuration>
    {
        private static readonly object                                  _logBufferLockObj       = new object();
        private readonly LoggingLevelSwitch                             _loggingLevelSwitch     = new LoggingLevelSwitch(LogEventLevel.Information);
        private readonly string                                         _buildFileVersion       = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyFileVersionAttribute), false)).Version;

        private Action                                                  _flushLogAction;
        private readonly StringBuilder                                  _logEventBuffer         = new StringBuilder();


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
            ConfigureLogging(Configuration?.LoggingConfiguration?.LogLevel ?? "OFF");

            Log.Information("VaultApplication {ApplicationName} {BuildVersion} has been initialized", ApplicationDefinition.Name, _buildFileVersion);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!
        }


        /// <summary>
        /// Build a Serilog structured logger from the configured log level
        /// </summary>
        /// <param name="logLevelString"></param>
        private void ConfigureLogging(string logLevelString)
        {
            _loggingLevelSwitch.MinimumLevel = GetLoggingLevelFor(logLevelString);

            // ------------------------------------------------------------------------------------------------------------------------------------
            // Build a Serilog logger with MFilesObjectLogSink.
            // Note to Log.CloseAndFlush() in the UninitializeApplication()!
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch)
                // Using a delegate to buffer log messages that are flushed later with a background job
                .WriteTo.DelegatingTextSink(w => WriteToVaultApplicationBuffer(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();


            // *WARNING* UNFORTUNATELY, the MFilesObjectlogSink CANNOT be used directly in a vault application like below.
            // Unfortunately, we have to use the DelegatingTextSink above to collect log events and a vault application
            // background job that flushes the collected messages after 5 seconds via an Action() to LogObjectRepository that
            // uses the PermanentVault reference...
            //
            // I nor Craig could not find any other way create a Serilog logger *centrally once* with a vault reference that is
            // not released after vault app startup... Not event using the vault reference from an event handler* EventHandlerEnvironment
            // works, because that vault reference is released before the batched sink uses it....
            //
            // And I hear you think, "how about the PermanentVault"? Unfortunately, the PermanentVault is not constructed yet and
            // may yield a very long vault app startup time and even errors.
            //
            // SO, DON'T build a MFilesLogObjectMessage nor MFilesLogFile logger here:
            //.WriteTo.MFilesLogObjectMessage(vault,
            //                                mfilesLogObjectNamePrefix:  $"[{Environment.MachineName.ToUpperInvariant()}] {Configuration?.LoggingConfiguration?.LogObjectNamePrefix}",
            //                                mfilesLogObjectTypeAlias:      Configuration?.LoggingConfiguration?.LogOT.Alias,
            //                                mfilesLogClassAlias:           Configuration?.LoggingConfiguration?.LogCL.Alias,
            //                                mfilesLogMessagePropDefAlias:  Configuration?.LoggingConfiguration?.LogMessagePD.Alias,
            //                                outputTemplate:                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
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
            if (oldConfiguration?.LoggingConfiguration?.LogLevel != Configuration?.LoggingConfiguration?.LogLevel)
            {
                _loggingLevelSwitch.MinimumLevel = GetLoggingLevelFor(Configuration?.LoggingConfiguration?.LogLevel);

                Log.Information("Admin changed Log level to {LogLevel}", Configuration?.LoggingConfiguration?.LogLevel);
            }
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
            // Define the delegate action for the flushing to the M-Files Log Object and/or Log File document object
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

                    // Check the Logging Configuration (see MF Admin)
                    if (null == Configuration || null == Configuration.LoggingConfiguration ||
                    !Configuration.LoggingConfiguration.LogOT.IsResolved ||
                    !Configuration.LoggingConfiguration.LogCL.IsResolved ||
                    !Configuration.LoggingConfiguration.LogMessagePD.IsResolved ||
                    !Configuration.LoggingConfiguration.LogFileCL.IsResolved ||
                    String.IsNullOrWhiteSpace(Configuration.LoggingConfiguration.LogOT.Alias) ||
                    String.IsNullOrWhiteSpace(Configuration.LoggingConfiguration.LogCL.Alias) ||
                    String.IsNullOrWhiteSpace(Configuration.LoggingConfiguration.LogMessagePD.Alias) ||
                    String.IsNullOrWhiteSpace(Configuration.LoggingConfiguration.LogFileCL.Alias))
                    { return; }

                    var prefix = Configuration.LoggingConfiguration.LogObjectNamePrefix; // 'DemoVaultApp-Log-'
                    if (string.IsNullOrWhiteSpace(prefix)) { prefix = "DemoVaultApp-Log-"; }

                    // Write to today's "Log" object
                    var rollingLogObjectRepository = new LogObjectRepository(PermanentVault,
                                                            mfilesLogObjectNamePrefix:    $"[{Environment.MachineName.ToUpperInvariant()}] {prefix}",     // eg, "[LTVICTOR3] DemoVaultApp-Log-"
                                                            mfilesLogObjectTypeAlias:     Configuration.LoggingConfiguration.LogOT.Alias,
                                                            mfilesLogClassAlias:          Configuration.LoggingConfiguration.LogCL.Alias,
                                                            mfilesLogMessagePropDefAlias: Configuration.LoggingConfiguration.LogMessagePD.Alias);

                    rollingLogObjectRepository.SaveLogMessage(batchedLogMessages);


                    // AND write to todays "LogFile" document object as well, *for the fun of it*.
                    var rollingLogFileRepository = new LogFileRepository(PermanentVault,
                                                            mfilesLogFileNamePrefix: $"[{Environment.MachineName.ToUpperInvariant()}] {prefix}",     // eg, "[LTVICTOR3] DemoVaultApp-Log-")
                                                            mfilesLogFileClassAlias: Configuration.LoggingConfiguration.LogFileCL.Alias);
                    rollingLogFileRepository.SaveLogMessage(batchedLogMessages);

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
            Log.Information("VaultApplication {ApplicationName} {BuildVersion} is POWERING DOWN.", ApplicationDefinition.Name, _buildFileVersion);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!

            // IMPORTANT: flush any buffered messages
            _flushLogAction?.Invoke();

            // IMPORTANT as well: flush all sinks
            Log.CloseAndFlush();

            base.UninitializeApplication(vault);
        }


        // -----------------------------------------------------------------------------------------------------------------------------------
        // Proving the Serilog sink can also be used from a vault extension method.

        [VaultExtensionMethod("DemoVaultApp.LogInformation")]
        private string LogInformation(EventHandlerEnvironment env)
        {
            Log.Information(env.Input);
            return $"Logged the following Information message:\r\n{env.Input}";
        }

        [VaultExtensionMethod("DemoVaultApp.LogWarning")]
        private string LogWarning(EventHandlerEnvironment env)
        {
            Log.Warning(env.Input);
            return $"Logged the following warning message:\r\n{env.Input}";
        }

        [VaultExtensionMethod("DemoVaultApp.LogError")]
        private string LogError(EventHandlerEnvironment env)
        {
            Log.Error(env.Input);
            return $"Logged the following error message:\r\n{env.Input}";
        }


        // -----------------------------------------------------------------------------------------------------------------------------------
        // Proving the Serilog sink can also be used from an MF Admin domain menu command.
        // Add a sample command to the MF Admin configuration domain area of this vault application, where you can right click and trigger this command.
        private readonly CustomDomainCommand cmdTestLogMessageMenuItem = new CustomDomainCommand
        {
            ID              = "cmdTestLogMessage",
            Execute         = (context, operations) => operations.ShowMessage(context.Vault.ExtensionMethodOperations.ExecuteVaultExtensionMethod("DemoVaultApp.LogInformation", $"Testing, one, two, three. Logged with love from the vault application domain area.") + "\r\n\r\nNote that it may take 5-10 seconds to show up in the M-Files log object."),
            DisplayName     = "Logging: log a test message",
            Locations       = new List<ICommandLocation> { new DomainMenuCommandLocation(icon: "play") }
        };


        /// <summary>
        /// The command which will be executed.
        /// </summary>
        /// <remarks>The "Execute" method will be called when the command is clicked.</remarks>
        private readonly CustomDomainCommand refreshDashboardCommand = new CustomDomainCommand
        {
            ID = "cmdRefreshDashboard",
            Execute = (c, o) =>
            {
                o.RefreshDashboard();
            }
        };


        // -----------------------------------------------------------------------------------------------------------------------------------
        // Showing Log level and logging vault structure status in the Configuration dashboard in MF Admin
        /// <inheritdoc />
        public override string GetDashboardContent(IConfigurationRequestContext context)
        {
            // Reacquire the cached vault structure
            ReinitializeMetadataStructureCache(PermanentVault);

            FormattableString loggingStructureState = $"";

            if (null != Configuration && null != Configuration.LoggingConfiguration)
            {
                var loggingConfiguration = Configuration.LoggingConfiguration;
                if (loggingConfiguration.LogLevel == "OFF")
                {
                    loggingStructureState = $"Logging to set to <b>OFF</b>";
                }
                else if (!loggingConfiguration.LogOT.IsResolved ||
                    !loggingConfiguration.LogCL.IsResolved ||
                    !loggingConfiguration.LogMessagePD.IsResolved ||
                    !loggingConfiguration.LogFileCL.IsResolved)
                {
                    var missingLoggingStructureAliases = PermanentVault.GetMissingLoggingVaultStructure(loggingConfiguration.LogOT.Alias,
                                                                                                        loggingConfiguration.LogCL.Alias,
                                                                                                        loggingConfiguration.LogMessagePD.Alias,
                                                                                                        loggingConfiguration.LogFileCL.Alias);
                    if (missingLoggingStructureAliases.Count > 0)
                    {
                        loggingStructureState = $"Logging check: logging is configured, but some logging vault structure is MISSING from the vault. Please run \"DemoVault.AddLoggingStructure.exe\" to ensure logging structure to the vault and refresh the vaultapp (to reread structure).<br/>Current log level is {loggingConfiguration.LogLevel}";
                    }
                    else
                    {
                        loggingStructureState = $"Logging check: logging is configured and all logging vault structure is present in the vault.<br/>Current log level is {loggingConfiguration.LogLevel}. See the M-Files desktop app for Log object and/or Log File objects.";
                    }
                }
            }
            else
            {
                loggingStructureState = $"Logging check: logging has not yet been configured or logging vault structure is missing.<br/>Please run \"DemoVault.AddLoggingStructure.exe\" to ensure logging structure in the vault, refresh the vaultapp (to reread structure) and configure logging.";
            }


            // Create the surrounding dashboard.
            var dashboard = new StatusDashboard();

            // Create a panel showing when the dashboard was rendered.
            // Application name, version.
            // "vault has the vault structure for logging:

            var refreshPanel = new DashboardPanel();
            refreshPanel.SetInnerContent(loggingStructureState);

            // Add the refresh command to the panel, and the panel to the dashboard.
            refreshPanel.Commands.Add(DashboardHelper.CreateDomainCommand("Refresh log check", this.refreshDashboardCommand.ID));
            dashboard.AddContent(refreshPanel);

            return dashboard.ToString();
        }


        public override IEnumerable<CustomDomainCommand> GetCommands(IConfigurationRequestContext context)
        {
	        return new List<CustomDomainCommand>(base.GetCommands(context))
	        {
                cmdTestLogMessageMenuItem,
			    refreshDashboardCommand
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
            // Appending log events to the a Log File document will trigger this event again, so we need to block logging about *that* object.
            if (EventIsForLogFileDocument(env)) { return; }


            // Just log about the document object change

            // And in 5-10 seconds, logger this message will be visible in the M-Files Desktop app as an Log object and Log document file.


            // ... do other stuff ?
        }


        /// <summary>
        /// Check if the event is for a document object that has the configured Log File Class
        /// </summary>
        /// <param name="env">EventHandlerEnvironment for the event</param>
        /// <returns>false if the event is NOT for a document of the configured log file class, true if it is.</returns>
        private bool EventIsForLogFileDocument(EventHandlerEnvironment env)
        {
            return env.ObjVerEx.HasClass(Configuration?.LoggingConfiguration?.LogFileCL ?? DefaultLoggingVaultStructure.LogFileClassAlias);
        }


/*
        // ==============================================================================================================================================
        WARNING:
        USING THE MFILESLOGOBJECTMESSAGE OR MFILESLOGFILE SINKS IN THE EVENT HANDLER WON'T WORK, because the env.Vault reference
        is released before the batched sink is using it after 5-10 seconds...


        /// <summary>
        /// Sample event handler that logs check ins.
        /// </summary>
        /// <param name="env"></param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize, ObjectType = (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument)]
        public void BeforeCheckInChangesFinalizeUpdateLogDemo(EventHandlerEnvironment env)
        {
            // Appending log events to the a Log File document will trigger this event again, so we need to block logging about *that* object.
            if (EventIsForLogFileDocument(env)) { return; }

            var logger = LoggerFor(env.Vault);

            // Just log about the document object change
            logger.Information("User {User} has checked in document {DisplayID} at {TimeStamp}", env.CurrentUserID, env.DisplayID, DateTime.Now);

            // And in 5-10 seconds, logger this message will be visible in the M-Files Desktop app as an Log object and Log document file.


            // ... do other stuff ?
        }


        /// <summary>
        /// Build a Serilog logger with the supplied vault reference
        /// </summary>
        /// <param name="vault"></param>
        /// <returns></returns>
        private ILogger LoggerFor(IVault vault)
        {
            var prefix = Configuration?.LoggingConfiguration?.LogObjectNamePrefix ?? DefaultLoggingVaultStructure.LogObjectNamePrefix;
            if (string.IsNullOrWhiteSpace(prefix)) { prefix = "DemoVaultApp-Log-"; }

            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch)

                .WriteTo.MFilesLogObjectMessage(vault,
                                            mfilesLogObjectNamePrefix:      $"[{Environment.MachineName.ToUpperInvariant()}] {prefix}",
                                            mfilesLogObjectTypeAlias:       Configuration?.LoggingConfiguration?.LogOT?.Alias           ?? DefaultLoggingVaultStructure.LogObjectTypeAlias,
                                            mfilesLogClassAlias:            Configuration?.LoggingConfiguration?.LogCL?.Alias           ?? DefaultLoggingVaultStructure.LogClassAlias,
                                            mfilesLogMessagePropDefAlias:   Configuration?.LoggingConfiguration?.LogMessagePD?.Alias    ?? DefaultLoggingVaultStructure.LogMessagePropertyDefinitionAlias,
                                            outputTemplate:                 "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

                // AND build an MFiles LogFile sink, for the fun of it
                .WriteTo.MFilesLogFile(vault,
                                            mfilesLogFileNamePrefix:        $"[{Environment.MachineName.ToUpperInvariant()}] {prefix}",
                                            mfilesLogFileClassAlias:        Configuration?.LoggingConfiguration?.LogFileCL?.Alias       ?? DefaultLoggingVaultStructure.LogFileClassAlias,
                                            outputTemplate:                 "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            return logger;
        }
*/

    }
}