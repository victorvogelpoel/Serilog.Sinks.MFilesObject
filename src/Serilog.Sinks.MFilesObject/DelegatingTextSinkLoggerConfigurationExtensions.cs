using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.Delegating;

namespace Serilog
{
    public static class DelegatingTextSinkLoggerConfigurationExtensions
    {
        public static LoggerConfiguration DelegatingTextSink(
                    this LoggerSinkConfiguration loggerSinkConfiguration,
                    Action<String> write,
                    LogEventLevel restrictedToMinimumLevel      = LevelAlias.Minimum,
                    string outputTemplate                       = MFilesObjectLoggerConfigurationExtensions.DefaultMFilesObjectOutputTemplate,
                    IFormatProvider formatProvider              = null,
                    LoggingLevelSwitch levelSwitch              = null
                    )
        {
            if (loggerSinkConfiguration == null)    throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (write == null)                      throw new ArgumentNullException(nameof(write));
            if (outputTemplate == null)             throw new ArgumentNullException(nameof(outputTemplate));

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return loggerSinkConfiguration.Sink(new DelegatingTextLogSink(write, formatter), restrictedToMinimumLevel, levelSwitch);
        }
    }
}
