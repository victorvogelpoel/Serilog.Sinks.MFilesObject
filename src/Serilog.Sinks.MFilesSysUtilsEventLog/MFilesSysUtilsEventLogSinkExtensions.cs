using System;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using Serilog.Sinks.MFilesSysUtilsEventLog;

namespace Serilog
{
    public static class MFilesSysUtilsEventLogSinkExtensions
    {
        public static LoggerConfiguration MFilesSysUtilsEventLogSink(this LoggerSinkConfiguration loggerSinkConfiguration, LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum, IFormatProvider formatProvider = null, LoggingLevelSwitch levelSwitch = null)
        {
            return loggerSinkConfiguration.Sink(new MFilesSysUtilsEventLogSink(formatProvider), restrictedToMinimumLevel, levelSwitch);
        }
    }
}
