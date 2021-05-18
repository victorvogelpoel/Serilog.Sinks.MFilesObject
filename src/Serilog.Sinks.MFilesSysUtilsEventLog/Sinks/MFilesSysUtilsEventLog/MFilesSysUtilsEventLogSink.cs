using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using MFiles.VAF.Common;

namespace Serilog.Sinks.MFilesSysUtilsEventLog
{
    public class MFilesSysUtilsEventLogSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public MFilesSysUtilsEventLogSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level == LogEventLevel.Debug|| logEvent.Level == LogEventLevel.Verbose)
            {
                return;
            }

            var message = logEvent.RenderMessage(_formatProvider);

            var entryType = System.Diagnostics.EventLogEntryType.Information;
            if (logEvent.Level == LogEventLevel.Warning)
            {
                entryType = System.Diagnostics.EventLogEntryType.Warning;
            }
            else if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Fatal)
            {
                entryType = System.Diagnostics.EventLogEntryType.Error;
            }

            SysUtils.ReportToEventLog(message, entryType);
        }
    }


    public static class MFilesSysUtilsEventLogSinkExtensions
    {
        public static LoggerConfiguration MFilesSysUtilsEventLogSink(this LoggerSinkConfiguration loggerSinkConfiguration, LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum, IFormatProvider formatProvider = null, LoggingLevelSwitch levelSwitch = null)
        {
            return loggerSinkConfiguration.Sink(new MFilesSysUtilsEventLogSink(formatProvider), restrictedToMinimumLevel, levelSwitch);
        }
    }
}
