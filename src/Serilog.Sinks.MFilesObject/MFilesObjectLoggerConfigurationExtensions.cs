// MFilesObjectLoggerConfigurationExtensions.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dramatic.LogToMFiles;
using MFilesAPI;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.MFilesObject;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog
{
    public static class MFilesObjectLoggerConfigurationExtensions
    {
        public const string DefaultMFilesObjectOutputTemplate                   = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";


        /// <summary>
        /// Configuration extension to configure the MFilesObject sink.
        /// </summary>
        /// <remarks>
        /// The MFilesLogObjectMessageSink cannot be used directly in a VaultApplication, because we don't control the lifetime of vault that is referenced at logger construction.
        /// This will yield a "COM object that has been separated from its underlying RCW cannot be used." error, should the logger be used.
        /// You cannot use the vaultPersistent nor this.PersistentVault as a argument in MFilesObject sink configuration builder.
        ///
        /// INSTEAD, in a vault application, use a Serilog DelegatingTextSink that drives an Action to buffer log messages. A background task in the vault application
        /// is used to flush these buffered messages every 5 seconds to an M-Files object through a MFilesLogRepository class. The PermanentVault is used as a reference to the vault.
        /// See the sample vault application in the Serilog.Sinks.MFilesObject for guidance.
        /// </remarks>
        /// <param name="loggerSinkConfiguration">The Serilog configuration builder to add Serilog.Sinks.MFilesObject sink configuration to</param>
        /// <param name="vault">An M-Files vault application reference (see remarks)</param>
        /// <param name="mfilesLogObjectNamePrefix">Prefix text for the Log objects, default "Log-"; Property NameOrTilte of a Log object will set to "Log-yyyy-MM-dd"</param>
        /// <param name="mfilesLogObjectTypeAlias">M-Files alias for the Log ObjectType, default "OT.Serilog.MFilesObjectLogSink.Log"</param>
        /// <param name="mfilesLogClassAlias">M_Files alias for the Log Class, default, "CL.Serilog.MFilesObjectLogSink.Log"</param>
        /// <param name="mfilesLogMessagePropDefAlias">M-Files alias for the LogMessage property definition, default "PD.Serilog.MFilesObjectLogSink.LogMessage"</param>
        /// <param name="restrictedToMinimumLevel">The minimal event log level that this sink emits for.</param>
        /// <param name="batchPostingLimit">The maximum number of events to include in a single batch. The default is 1000</param>
        /// <param name="period">The time to wait between checking for event batches. The default is two seconds.</param>
        /// <param name="queueSizeLimit">Maximum number of events to hold in the sink's internal queue, or null for an unbounded queue. The default is 10000.</param>
        /// <param name="outputTemplate">A message template describing the output messages.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="controlLevelSwitch">If provided, the switch will be updated based on the log level setting in vault application</param>
        /// <returns>A LoggerSinkConfiguration with configuration for MFilesObject sink added.</returns>
        public static LoggerConfiguration MFilesObjectLogMessage(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            IVault vault,
            string mfilesLogObjectNamePrefix        = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogObjectNamePrefix,
            string mfilesLogObjectTypeAlias         = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogObjectTypeAlias,
            string mfilesLogClassAlias              = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogClassAlias,
            string mfilesLogMessagePropDefAlias     = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogMessagePropertyDefinitionAlias,
            LogEventLevel restrictedToMinimumLevel  = LevelAlias.Minimum,
            int batchPostingLimit                   = MFilesLogObjectMessageSink.DefaultBatchPostingLimit,
            TimeSpan? period                        = null,
            string outputTemplate                   = MFilesObjectLoggerConfigurationExtensions.DefaultMFilesObjectOutputTemplate,
            IFormatProvider formatProvider          = null,
            int queueSizeLimit                      = MFilesLogObjectMessageSink.DefaultQueueSizeLimit
            )
        {

            if (loggerSinkConfiguration == null)    throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (vault == null)                      throw new ArgumentNullException(nameof(vault));
            if (outputTemplate == null)             throw new ArgumentNullException(nameof(outputTemplate));
            if (queueSizeLimit < 0)                 throw new ArgumentOutOfRangeException(nameof(queueSizeLimit), "Queue size limit must be non-zero.");

            var defaultedPeriod                     = period ?? MFilesLogObjectMessageSink.DefaultPeriod;
            var formatter                           = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            // Create the M-Files object sink
            var mfilesSink                          = new MFilesLogObjectMessageSink(vault, mfilesLogObjectNamePrefix, mfilesLogObjectTypeAlias, mfilesLogClassAlias, mfilesLogMessagePropDefAlias, formatter);

            var options = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit                      = batchPostingLimit,
                Period                              = defaultedPeriod,
                QueueLimit                          = queueSizeLimit
            };

            // Wrap the MFilesObjectLogSink into a PeriodicBatchingSink for batching emitting events to an M-Files Log object.
            ILogEventSink batchedSink               = new PeriodicBatchingSink(mfilesSink, options);

            return loggerSinkConfiguration.Sink(batchedSink, restrictedToMinimumLevel);
        }


        /// <summary>
        /// Configuration extension to configure the MFilesObject sink.
        /// </summary>
        /// <remarks>
        /// The MFilesLogObjectFileSink cannot be used directly in a VaultApplication, because we don't control the lifetime of vault that is referenced at logger construction.
        /// This will yield a "COM object that has been separated from its underlying RCW cannot be used." error, should the logger be used.
        /// You cannot use the vaultPersistent nor this.PersistentVault as a argument in MFilesObject sink configuration builder.
        ///
        /// INSTEAD, in a vault application, use a Serilog DelegatingTextSink that drives an Action to buffer log messages. A background task in the vault application
        /// is used to flush these buffered messages every 5 seconds to an M-Files object through a MFilesLogRepository class. The PermanentVault is used as a reference to the vault.
        /// See the sample vault application in the Serilog.Sinks.MFilesObject for guidance.
        /// </remarks>
        /// <param name="loggerSinkConfiguration">The Serilog configuration builder to add Serilog.Sinks.MFilesObject sink configuration to</param>
        /// <param name="vault">An M-Files vault application reference (see remarks)</param>
        /// <param name="mfilesLogObjectNamePrefix">Prefix text for the Log objects, default "Log-"; Property NameOrTilte of a Log object will set to "Log-yyyy-MM-dd"</param>
        /// <param name="mfilesLogObjectTypeAlias">M-Files alias for the Log ObjectType, default "OT.Serilog.MFilesObjectLogSink.Log"</param>
        /// <param name="mfilesLogClassAlias">M_Files alias for the Log Class, default, "CL.Serilog.MFilesObjectLogSink.Log"</param>
        /// <param name="restrictedToMinimumLevel">The minimal event log level that this sink emits for.</param>
        /// <param name="batchPostingLimit">The maximum number of events to include in a single batch. The default is 1000</param>
        /// <param name="period">The time to wait between checking for event batches. The default is two seconds.</param>
        /// <param name="queueSizeLimit">Maximum number of events to hold in the sink's internal queue, or null for an unbounded queue. The default is 10000.</param>
        /// <param name="outputTemplate">A message template describing the output messages.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="controlLevelSwitch">If provided, the switch will be updated based on the log level setting in vault application</param>
        /// <returns>A LoggerSinkConfiguration with configuration for MFilesObject sink added.</returns>
        public static LoggerConfiguration MFilesLogFile(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            IVault vault,
            string mfilesLogFileNamePrefix          = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogFileNamePrefix,
            string mfilesLogFileClassAlias          = MFilesObjectLogSinkVaultStructure.DefaultMFilesLogClassAlias,
            LogEventLevel restrictedToMinimumLevel  = LevelAlias.Minimum,
            int batchPostingLimit                   = MFilesLogFileSink.DefaultBatchPostingLimit,
            TimeSpan? period                        = null,
            string outputTemplate                   = MFilesObjectLoggerConfigurationExtensions.DefaultMFilesObjectOutputTemplate,
            IFormatProvider formatProvider          = null,
            int queueSizeLimit                      = MFilesLogFileSink.DefaultQueueSizeLimit
            )
        {

            if (loggerSinkConfiguration == null)    throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (vault == null)                      throw new ArgumentNullException(nameof(vault));
            if (outputTemplate == null)             throw new ArgumentNullException(nameof(outputTemplate));
            if (queueSizeLimit < 0)                 throw new ArgumentOutOfRangeException(nameof(queueSizeLimit), "Queue size limit must be non-zero.");

            var defaultedPeriod                     = period ?? MFilesLogFileSink.DefaultPeriod;
            var formatter                           = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            // Create the M-Files object sink
            var mfilesSink                          = new MFilesLogFileSink(vault, mfilesLogFileNamePrefix, mfilesLogFileClassAlias, formatter);

            var options = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit                      = batchPostingLimit,
                Period                              = defaultedPeriod,
                QueueLimit                          = queueSizeLimit
            };

            // Wrap the MFilesObjectLogSink into a PeriodicBatchingSink for batching emitting events to an M-Files Log object.
            ILogEventSink batchedSink               = new PeriodicBatchingSink(mfilesSink, options);

            return loggerSinkConfiguration.Sink(batchedSink, restrictedToMinimumLevel);
        }

    }
}
