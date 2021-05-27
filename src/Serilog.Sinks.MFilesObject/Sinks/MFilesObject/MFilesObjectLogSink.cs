// MFilesObjectLogSink.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MFilesAPI;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MFilesObject
{
    public class MFilesObjectLogSink : IBatchedLogEventSink
    {
        internal const string DefaultMFilesLogMessagePropertyDefinitionAlias    = "PD.Serilog.MFilesObjectLogSink.LogMessage";
        internal const string DefaultMFilesLogObjectTypeAlias                   = "OT.Serilog.MFilesObjectLogSink.Log";
        internal const string DefaultMFilesLogClassAlias                        = "CL.Serilog.MFilesObjectLogSink.Log";
        internal const string DefaultMFilesLogObjectNamePrefix                  = "Log-";


        public const int DefaultBatchPostingLimit                               = 1000;
        public const int DefaultQueueSizeLimit                                  = 100000;
        public static readonly TimeSpan DefaultPeriod                           = TimeSpan.FromSeconds(5);
        //static readonly TimeSpan RequiredLevelCheckInterval                     = TimeSpan.FromMinutes(2);
        //private DateTime _nextRequiredLevelCheckUtc                             = DateTime.UtcNow.Add(RequiredLevelCheckInterval);

        private readonly ControlledLevelSwitch _controlledSwitch;
        private readonly MFilesLogRepository _mfilesLogRepository;
        private readonly ITextFormatter _formatter;

        /// <summary>
        /// Construct an M-Files object log sink with parameters
        /// </summary>
        /// <param name="vault">M-Files vault application</param>
        /// <param name="mfilesLogObjectNamePrefix">Prefix for the Log object Name-or-Title. You may want to use the name of the VaultApplication.</param>
        /// <param name="mfilesLogObjectTypeAlias">Alias for the Log ObjectType</param>
        /// <param name="mfilesLogClassAlias">Alias for the Log ClassObject</param>
        /// <param name="mfilesLogMessagePropDefAlias">Alias for the LogMessage PropertyDefinition</param>
        /// <param name="controlledSwitch">Serilog switch to use for minimal log level</param>
        /// <param name="formatter">a text formatter for converting the log event into a string with event arguments</param>
        public MFilesObjectLogSink(IVault vault, string mfilesLogObjectNamePrefix, string mfilesLogObjectTypeAlias, string mfilesLogClassAlias, string mfilesLogMessagePropDefAlias, ControlledLevelSwitch controlledSwitch, ITextFormatter formatter)
        {
            //_controlledSwitch       = controlledSwitch ?? throw new ArgumentNullException(nameof(controlledSwitch));
            _formatter              = formatter ?? throw new ArgumentNullException(nameof(formatter));

            _mfilesLogRepository    = new MFilesLogRepository(vault, mfilesLogObjectNamePrefix, mfilesLogObjectTypeAlias, mfilesLogClassAlias, mfilesLogMessagePropDefAlias);
        }

        //public async Task OnEmptyBatchAsync()
        //{
        //    if (_controlledSwitch.IsActive && _nextRequiredLevelCheckUtc < DateTime.UtcNow)
        //    {
        //        await EmitBatchAsync(Enumerable.Empty<LogEvent>());
        //    }
        //}

        public Task OnEmptyBatchAsync()
        {
            return Task.FromResult(0);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="batch"></param>
        /// <returns></returns>
        public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            var batchedMessage = new StringBuilder();
            foreach (var logEvent in batch.Where(logEvent => logEvent.Level != LogEventLevel.Debug && logEvent.Level != LogEventLevel.Verbose))
            {
                // Format the event according to the OutputTemplate, eg "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                // see https://github.com/serilog/serilog-sinks-console#output-templates

                using (var s = new StringWriter())
                {
                    _formatter.Format(logEvent, s);

                    batchedMessage.AppendLine(s.ToString());
                }
            }

            try
            {
                _mfilesLogRepository.WriteLogMessage(batchedMessage.ToString());
            }
            catch (Exception ex)
            {
                throw;  // Keeping this to help me debug M-Files / Serilog exceptions; TODO: remove the catch at sink release.
            }

            return Task.FromResult(0);
        }
    }
}

