﻿// MFilesObjectLogSink.cs
// 14-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dramatic.LogToMFiles;
using MFilesAPI;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MFilesObject
{
    public class MFilesLogFileSink : IBatchedLogEventSink
    {
        public const int DefaultBatchPostingLimit                           = 1000;
        public const int DefaultQueueSizeLimit                              = 100000;
        public static readonly TimeSpan DefaultPeriod                       = TimeSpan.FromSeconds(10);

        private readonly MFilesLogFileRepository _mfilesLogRepository;
        private readonly ITextFormatter _formatter;

        /// <summary>
        /// Construct an M-Files object log sink with parameters
        /// </summary>
        /// <param name="vault">M-Files vault application</param>
        /// <param name="mfilesLogFileNamePrefix">Prefix for the Log object Name-or-Title. You may want to use the name of the VaultApplication.</param>
        /// <param name="formatter">a text formatter for converting the log event into a string with event arguments</param>
        public MFilesLogFileSink(IVault vault, string mfilesLogFileNamePrefix, string mfilesLogFileClassAlias, ITextFormatter formatter)
        {
            _formatter              = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _mfilesLogRepository    = new MFilesLogFileRepository(vault, mfilesLogFileNamePrefix, mfilesLogFileClassAlias);
        }

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

                    batchedMessage.AppendLine(s.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
                }
            }

            try
            {
                _mfilesLogRepository.WriteLogFile(batchedMessage.ToString());
            }
            catch (Exception ex) when (Debugger.IsAttached)
            {
                _ = ex;
                Debugger.Break();   // Keeping this to help me debug M-Files / Serilog exceptions

                throw;
            }

            return Task.FromResult(0);
        }
    }
}

