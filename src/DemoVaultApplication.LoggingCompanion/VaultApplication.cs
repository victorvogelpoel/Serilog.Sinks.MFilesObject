using System;
using System.Diagnostics;
using Dramatic.LogToMFiles;
using MFiles.VAF;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Core;
using MFilesAPI;

namespace DemoVaultApplication.LoggingCompanion
{
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>
    public class VaultApplication : ConfigurableVaultApplicationBase<Configuration>
    {
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
            // Ensure that the structure for the logging-to-object is present in the vault, create if necessary.
            vault.EnsureLoggingVaultStructure(_loggingStructureConfig);
        }

        /// <summary>
        /// Power down the vault application. At least, flush the logging sinks.
        /// </summary>
        /// <param name="vault"></param>
        protected override void UninitializeApplication(Vault vault)
        {
            vault.RemoveLogObjectsAndLoggingVaultStructure(_loggingStructureConfig);

            base.UninitializeApplication(vault);
        }
    }
}