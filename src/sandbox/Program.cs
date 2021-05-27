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


                // Add Vault structure for logging if it isn't there: OT "Log", CL "Log" and PD "LogMessage"
                var structureConfig = new MFilesObjectLogSinkVaultStructureConfiguration
                {
                    LogObjectTypeNameSingular   = "Log",
                    LogObjectTypeNamePlural     = "Logs",
                    LogMessagePropDefName       = "LogMessage",

                    LogObjectTypeAlias          = "OT.Serilog.MFilesObjectLogSink.Log",
                    LogClassAlias               = "CL.Serilog.MFilesObjectLogSink.Log",
                    LogMessagePropDefAlias      = "PD.Serilog.MFilesObjectLogSink.LogMessage"
                };

                // Ensure that the structure for Logging object and class is present in the vault
                vault.EnsureLogSinkVaultStructure(structureConfig);


                // -------------------------------------------------------------------------------------------------------
                // Now the fun starts!
                var loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

                // Build a Serilog logger with MFilesObjectLogSink and Console
                Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.ControlledBy(loggingLevelSwitch)
                    .WriteTo.DelegatingTextSink(w => WriteToBuffer(w), outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", levelSwitch:loggingLevelSwitch)
                    .WriteTo.MFilesObject(vault, mfilesLogObjectNamePrefix:"LoggingFromSandboxDemo-Log-")       // Log to an 'rolling' object in the vault, eg objectType "Log" with a multiline text property.
                    .WriteTo.Console()                                                                          // Write to the console with the same Log.xx statements to see them in the console terminal :-)
                    .CreateLogger();


                // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
                // Now log messages
                // NOTE that these messages are BATCHED and stored in an new or existing M-Files object with the name "Log-yyyy-MM-dd" every 5 seconds.
                // Log messages do NOT appear immediately in the vault as a Log object, but are collected and pushed every 5 secs.
                Log.Information("This adds this log message to a Log object in the vault with the name \"DemoSandbox-Log-{Today}\"", DateTime.Today.ToString("yyyy-MM-dd"));

                Log.Information("This adds another info message to the {LogOT} object", structureConfig.LogObjectTypeNameSingular); // NOTE, structured logging, NOT C# string intrapolation!

                Log.Warning("And now a warning");

                Log.Error("And an ERROR!");

                // NOTE: in M-Files vault desktop application, explicitly search for ObjectType = "Log"

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
        private static void WriteToBuffer(string formattedLogMessage)
        {
            _logEventBuffer.AppendLine(formattedLogMessage);
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
