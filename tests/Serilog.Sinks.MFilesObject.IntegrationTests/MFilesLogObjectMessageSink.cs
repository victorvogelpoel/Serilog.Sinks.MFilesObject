using System;
using System.Threading;
using Dramatic.LogToMFiles;
using FluentAssertions;
using MFilesAPI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.MFilesObject.IntegrationTests
{
    [TestClass]
    public class MFilesLogObjectMessageSinkTests
    {
        private readonly MFilesObjectLoggingVaultStructureConfiguration _loggingConfiguration = new MFilesObjectLoggingVaultStructureConfiguration
            {
                LogObjectTypeNameSingular   = "Log",
                LogObjectTypeNamePlural     = "Logs",
                LogMessagePropDefName       = "LogMessage",

                LogObjectTypeAlias          = "OT.Serilog.MFilesObjectLogSink.Log",
                LogClassAlias               = "CL.Serilog.MFilesObjectLogSink.Log",
                LogMessagePropDefAlias      = "PD.Serilog.MFilesObjectLogSink.LogMessage"
            };




        public Vault GetPreparedVault()
        {
            var serverApp           = new MFilesAPI.MFilesServerApplication();
            serverApp.Connect(MFilesAPI.MFAuthType.MFAuthTypeLoggedOnWindowsUser);
            var vaultOnServer       = serverApp.GetOnlineVaults().GetVaultByName("Serilog.Sinks.MFilesObject"); // The "Serilog.Sinks.MFilesObject" demo vault that mysteriously bears the same name as the logging solution
            var vault               = serverApp.LogInAsUserToVault(vaultOnServer.GUID);  // "{D449E438-89EE-42BB-9769-B862E9B1B140}"

            //vault.RemoveLogObjectsAndLoggingVaultStructure(_loggingConfiguration);

            // Ensure that the structure for Logging object and class is present in the vault (needs full permissions on vault)
            vault.EnsureLoggingVaultStructure(_loggingConfiguration);

            return vault;
        }


        [TestMethod]
        public void test()
        {
            // ASSIGN
            var vault                   = GetPreparedVault();
            var mfilesLogObjectTypeID   = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(_loggingConfiguration.LogObjectTypeAlias);

            var expecte

            // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // ACT

            // Define the minimal log level for the log pipeline; any log level below this (eg Verbose, Debug), will not go through.
            var loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            // Build a Serilog logger with MFilesObjectLogSink and Console
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)

                //// Log events to an 'rolling' Log object in the vault with a MultiLineText property.
                .WriteTo.MFilesLogObjectMessage(vault,
                                        mfilesLogObjectNamePrefix:      "Serilog.Sinks.MFilesObject.IntegrationTests-",
                                        mfilesLogObjectTypeAlias:       _loggingConfiguration.LogObjectTypeAlias,
                                        mfilesLogClassAlias:            _loggingConfiguration.LogClassAlias,
                                        mfilesLogMessagePropDefAlias:   _loggingConfiguration.LogMessagePropDefAlias,
                                        outputTemplate:                 "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

                // Write to colored console terminal :-)
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Test message at {Now}\"", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Wait 10 seconds to make sure the batched log messages
            Thread.Sleep(TimeSpan.FromSeconds(10));

            var logObjectSearchResults = vault.SearchAllLogObjects(mfilesLogObjectTypeID);

            // -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // ASSERT

            vault.HasLoggingVaultStructure(_loggingConfiguration).Should().BeTrue();

            logObjectSearchResults.Count.Should().Be(1);

        }
    }
}
