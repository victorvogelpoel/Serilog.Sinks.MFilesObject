// Program.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dramatic.LogToMFiles;
using MFilesAPI;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MFilesObject;

namespace SANDBOX
{
    class Program
    {
        static void Main(string[] args)
        {
            // -------------------------------------------------------------------------------------------------------
            // IMPORTANT: THIS IS A PERSONAL LAB PROJECT BY VICTOR VOGELPOEL. YMMV!

            try
            {
                var serverApp           = new MFilesAPI.MFilesServerApplication();
                serverApp.Connect(MFilesAPI.MFAuthType.MFAuthTypeLoggedOnWindowsUser);
                var vault               = serverApp.LogInAsUserToVault("{D449E438-89EE-42BB-9769-B862E9B1B140}");  // The "Serilog.Sinks.MFilesObject" demo vault


                // Define vault structure for logging if it isn't there: OT "Log", CL "Log" and PD "LogMessage" and aliases to find them back.
                var structureConfig = new MFilesObjectLogSinkVaultStructureConfiguration
                {
                    LogObjectTypeNameSingular   = "Log",
                    LogObjectTypeNamePlural     = "Logs",
                    LogMessagePropDefName       = "LogMessage",

                    LogObjectTypeAlias          = "OT.Serilog.MFilesObjectLogSink.Log",
                    LogClassAlias               = "CL.Serilog.MFilesObjectLogSink.Log",
                    LogMessagePropDefAlias      = "PD.Serilog.MFilesObjectLogSink.LogMessage"
                };

                // Ensure that the structure for Logging object and class is present in the vault (needs full permissions on vault)
                vault.EnsureLogSinkVaultStructure(structureConfig);


                // -------------------------------------------------------------------------------------------------------
                // Now the fun starts!

                // Define the minimal log level for the log pipeline; any log level below this (eg Verbose, Debug), will not go through.
                var loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

                // Build a Serilog logger with MFilesObjectLogSink and Console
                Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.ControlledBy(loggingLevelSwitch)

                    // Sample DelegatingTextSink to buffer formatted log events, all log levels
                    .WriteTo.DelegatingTextSink(w => BufferAllLogEvents(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

                    // Sample DelegatingTextSink to buffer formatted ERROR log events (ONLY ERROR level or above).
                    .WriteTo.DelegatingTextSink(w => BufferErrorEvents(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: LogEventLevel.Error)

                    // Log events to an 'rolling' Log object in the vault with a MultiLineText property.
                    .WriteTo.MFilesObject(  vault,
                                            mfilesLogObjectNamePrefix:      "LoggingFromSandboxDemo-Log-",
                                            mfilesLogObjectTypeAlias:       structureConfig.LogObjectTypeAlias,
                                            mfilesLogClassAlias:            structureConfig.LogClassAlias,
                                            mfilesLogMessagePropDefAlias:   structureConfig.LogMessagePropDefAlias,
                                            outputTemplate:                 "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

                    // Write to colored console terminal :-)
                    .WriteTo.Console()

                    //.WriteTo.File(@"c:\somepath\Log-.txt",
                    //                outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    //                restrictedToMinimumLevel:LogEventLevel.Information,
                    //                rollingInterval: RollingInterval.Day)

                    .CreateLogger();


                // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
                // Now log messages
                // With above configuration, each Log.xxxx() statement will log to the delegating sinks AND and M-Files Log object, AND to the console.
                // Note that the Log messages do NOT appear immediately in the vault as a Log object, but are collected and pushed every 5 secs.

                Log.Information("This adds this log message to a Log object in the vault with the name \"LoggingFromSandboxDemo-Log-{Today}\"", DateTime.Today.ToString("yyyy-MM-dd"));

                Log.Information("This adds another info message to the {LogOT} object", structureConfig.LogObjectTypeNameSingular);     // NOTE, structured logging, NOT C# string intrapolation!

                Log.Warning("And now a warning");

                Log.Error("And an ERROR!");

                // NOTE: in M-Files vault desktop application, navigate to objects of class "Log"

                Console.WriteLine("-----------------------------------------------------------------------------------------");
                Console.WriteLine("This is what the AllLogEvents DelegatingTextSink buffered:");
                Console.WriteLine(_logEventBuffer.ToString());
                Console.WriteLine("");
                Console.WriteLine("And this is what the ErrorEvents DelegatingTextSink buffered (should be ONLY errors):");
                Console.WriteLine(_errorLogEventBuffer.ToString());

                Thread.Sleep(6000);



                // IMPORTANT to flush out the batched messages to the vault, at the end of the application, otherwise messages within the last 5 seconds would not end up in the vault!
                Log.CloseAndFlush();

            }
            catch (Exception ex)
            {
                if (ex is AggregateException aggrEx)  // AggregateException is an compound exception when something failed in a async/task function, like a Validation.
                {
                    foreach (var innerEx in aggrEx.InnerExceptions)
                    {
                        OutputException(innerEx);
                    }
                }
                else
                {
                    OutputException(ex);
                }
            }

            Console.WriteLine("Hit enter to exit");
            Console.ReadLine();
        }




        private static StringBuilder _logEventBuffer = new StringBuilder();
        private static void BufferAllLogEvents(string formattedLogMessage)
        {
            _logEventBuffer.AppendLine(formattedLogMessage.TrimEnd(Environment.NewLine.ToCharArray()));
        }

        private static StringBuilder _errorLogEventBuffer = new StringBuilder();
        private static void BufferErrorEvents(string formattedLogMessage)
        {
            _errorLogEventBuffer.AppendLine(formattedLogMessage.TrimEnd(Environment.NewLine.ToCharArray()));
        }

        private static void OutputException(Exception ex)
        {
            Console.WriteLine("{1}:{0}{2}", Environment.NewLine, ex.GetType().FullName, ex.Message);

            if (null != ex.InnerException)
            {
                OutputException(ex.InnerException);
            }
        }
    }
}
