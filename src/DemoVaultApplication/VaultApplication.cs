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
            ConfigureLogging(Configuration?.LoggingConfiguration?.LogLevel);

            Log.Information("VaultApplication {ApplicationName} {BuildVersion} has been initialized", ApplicationDefinition.Name, _buildFileVersion);   // NOTE, structured logging with curly braces, NOT C# string intrapolation $"" with curly braces!
        }


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
            if (oldConfiguration?.LoggingConfiguration?.LogLevel != Configuration?.LoggingConfiguration?.LogLevel)
            {
                _loggingLevelSwitch.MinimumLevel = GetLoggingLevelFor(Configuration?.LoggingConfiguration?.LogLevel);
            }

            Log.Information("Admin changed Log level to {LogLevel}", Configuration?.LoggingConfiguration?.LogLevel);
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

                    if (null == Configuration.LoggingConfiguration ||
                        !Configuration.LoggingConfiguration.LogOT.IsResolved ||
                        !Configuration.LoggingConfiguration.LogCL.IsResolved ||
                        !Configuration.LoggingConfiguration.LogMessagePD.IsResolved ||
                        String.IsNullOrWhiteSpace(Configuration.LoggingConfiguration.LogOT.Alias) ||
                        String.IsNullOrWhiteSpace(Configuration.LoggingConfiguration.LogCL.Alias) ||
                        String.IsNullOrWhiteSpace(Configuration.LoggingConfiguration.LogMessagePD.Alias))
                    { return; }

                    var prefix = Configuration.LoggingConfiguration.LogObjectNamePrefix; // 'DemoVaultApp-Log-'
                    if (string.IsNullOrWhiteSpace(prefix)) { prefix = "DemoVaultApp-Log-"; }

                    var repository = new MFilesLogObjectRepository(PermanentVault,
                                                                   mfilesLogObjectNamePrefix:     $"[{Environment.MachineName.ToUpperInvariant()}] {prefix}",
                                                                   mfilesLogObjectTypeAlias:      Configuration.LoggingConfiguration.LogOT.Alias,
                                                                   mfilesLogClassAlias:           Configuration.LoggingConfiguration.LogCL.Alias,
                                                                   mfilesLogMessagePropDefAlias:  Configuration.LoggingConfiguration.LogMessagePD.Alias);

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



        // TODO: log method to be used from VBScript:
        [VaultExtensionMethod("SampleVaultApp.LogInformation")]
        private string LogInformation(EventHandlerEnvironment env)
        {
            Log.Information(env.Input);
            return $"Logged the following Information message:\r\n{env.Input}";
        }

        [VaultExtensionMethod("SampleVaultApp.LogWarning")]
        private string LogWarning(EventHandlerEnvironment env)
        {
            Log.Warning(env.Input);
            return $"Logged the following warning message:\r\n{env.Input}";
        }

        [VaultExtensionMethod("SampleVaultApp.LogError")]
        private string LogError(EventHandlerEnvironment env)
        {
            Log.Error(env.Input);
            return $"Logged the following error message:\r\n{env.Input}";
        }



        private readonly CustomDomainCommand cmdTestLogMessageMenuItem = new CustomDomainCommand
        {
            ID              = "cmdTestLogMessage",
            Execute         = (context, operations) => operations.ShowMessage(context.Vault.ExtensionMethodOperations.ExecuteVaultExtensionMethod("SampleVaultApp.LogInformation", $"[{DateTime.Now:HH:mm:ss} INF] Testing, one, two, three. Logged with love from the vault application domain area.")),
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
		    ConfirmMessage = "Are you sure you would like to refresh the dashboard?",
		    Execute = (c, o) =>
		    {
			    o.RefreshDashboard();
		    }
	    };



	    /// <inheritdoc />
	    public override string GetDashboardContent(IConfigurationRequestContext context)
	    {
		    // Create the surrounding dashboard.
		    var dashboard = new StatusDashboard();

		    // Create a panel showing when the dashboard was rendered.
		    var refreshPanel = new DashboardPanel();
		    refreshPanel.SetInnerContent( $"Dashboard generated at: {DateTime.Now.ToString( "T" )}" );

		    // Add the refresh command to the panel, and the panel to the dashboard.
		    refreshPanel.Commands.Add( DashboardHelper.CreateDomainCommand( "Refresh", this.refreshDashboardCommand.ID ) );
		    dashboard.AddContent( refreshPanel );

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
            using (LogContext.PushProperty("MFEventType", env.EventType.ToString()))  // Note "Enrich.FromLogContext()" in the configuration builder earlier!
            {
                // Now every log event in this scope automatically has this additional property "MFEventType" from the M-Files event handler environment!

                Log.Information("User {User} has checked in document {DisplayID} at {TimeStamp}", env.CurrentUserID, env.DisplayID, DateTime.Now);

                // ... do other stuff
            }
        }
    }
}