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
using MFilesAPI;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MFilesObject;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog
{
    public static class MFilesObjectLoggerConfigurationExtensions
    {
        /// <summary>
        /// Configuration extension to configure the MFilesObject sink.
        /// </summary>
        /// <param name="loggerSinkConfiguration"></param>
        /// <param name="vault"></param>
        /// <param name="mfilesLogObjectNamePrefix">Prefix text for the Log objects, default "Log-"; Property NameOrTilte of a Log object will set to "Log-yyyy-MM-dd"</param>
        /// <param name="mfilesLogObjectTypeAlias">M-Files alias for the Log ObjectType, default "OT.Serilog.MFilesObjectLogSink.Log"</param>
        /// <param name="mfilesLogClassAlias">M_Files alias for the Log Class, default, "CL.Serilog.MFilesObjectLogSink.Log"</param>
        /// <param name="mfilesLogMessagePropDefAlias">M-Files alias for the LogMessage property definition, default "PD.Serilog.MFilesObjectLogSink.LogMessage"</param>
        /// <param name="restrictedToMinimumLevel"></param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="period"></param>
        /// <param name="formatProvider"></param>
        /// <param name="controlLevelSwitch"></param>
        /// <param name="queueSizeLimit"></param>
        /// <returns></returns>
        public static LoggerConfiguration MFilesObject(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            IVault vault,
            string mfilesLogObjectNamePrefix        = MFilesObjectLogSink.DefaultMFilesLogObjectNamePrefix,
            string mfilesLogObjectTypeAlias         = MFilesObjectLogSink.DefaultMFilesLogObjectTypeAlias,
            string mfilesLogClassAlias              = MFilesObjectLogSink.DefaultMFilesLogClassAlias,
            string mfilesLogMessagePropDefAlias     = MFilesObjectLogSink.DefaultMFilesLogMessagePropertyDefinitionAlias,
            LogEventLevel restrictedToMinimumLevel  = LevelAlias.Minimum,
            int batchPostingLimit                   = MFilesObjectLogSink.DefaultBatchPostingLimit,
            TimeSpan? period                        = null,
            IFormatProvider formatProvider          = null,
            LoggingLevelSwitch controlLevelSwitch   = null,
            int queueSizeLimit                      = MFilesObjectLogSink.DefaultQueueSizeLimit
            )
        {
            if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (vault == null)                   throw new ArgumentNullException(nameof(vault));
            if (queueSizeLimit < 0)              throw new ArgumentOutOfRangeException(nameof(queueSizeLimit), "Queue size limit must be non-zero.");

            var defaultedPeriod     = period ?? MFilesObjectLogSink.DefaultPeriod;
            var controlledSwitch    = new ControlledLevelSwitch(controlLevelSwitch);
            var batchedSink         = new MFilesObjectLogSink(vault, mfilesLogObjectNamePrefix, mfilesLogObjectTypeAlias, mfilesLogClassAlias, mfilesLogMessagePropDefAlias, controlledSwitch, formatProvider);

            var options = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit  = batchPostingLimit,
                Period          = defaultedPeriod,
                QueueLimit      = queueSizeLimit
            };

            ILogEventSink sink = new PeriodicBatchingSink(batchedSink, options);

            return loggerSinkConfiguration.Conditional(controlledSwitch.IsIncluded, wt => wt.Sink(sink, restrictedToMinimumLevel));
        }
    }
}
