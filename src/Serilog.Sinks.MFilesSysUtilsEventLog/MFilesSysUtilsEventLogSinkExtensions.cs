// MFilesSysUtilsEventLogSinkExtensions.cs
// 18-5-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
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
