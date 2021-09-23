using System;
using System.Collections.Generic;
using Dramatic.LogToMFiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace Serilog.Sinks.MFilesObject.Tests
{
    [TestClass]
    public class MFilesLogFileSinkTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            // ASSIGN
            var outputTemplate          = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";
            var logObjectRepositoryMock = new Mock<ILogMessageRepository>();

            var formatter               = new MessageTemplateTextFormatter(outputTemplate, null);
            var sink                    = new MFilesLogFileSink(logObjectRepositoryMock.Object, formatter);

            var parser                  = new MessageTemplateParser();
            var messageTemplate1        = parser.Parse("Status {Status}");
            var messageTemplate2        = parser.Parse("Some message");

            var batchLogEvents = new List<LogEvent>
            {
                new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, exception:null, messageTemplate1, properties:new List<LogEventProperty>()),
                new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Warning, exception:null, messageTemplate2, properties:new List<LogEventProperty>())
            };

            // ACTION
            sink.EmitBatchAsync(batchLogEvents);

            // ASSERT
            logObjectRepositoryMock.Verify(m=>m.SaveLogMessage("[INF] Status {Status}\r\n[WRN] Some message\r\n"), Times.Once);
        }
    }
}
