using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Delegating
{
    public class DelegatingTextLogSink : ILogEventSink
    {
        private readonly Action<String> _write;
        private readonly ITextFormatter _formatter;

        public DelegatingTextLogSink(Action<String> write, ITextFormatter formatter)
        {
            _write      = write ?? throw new ArgumentNullException(nameof(write));
            _formatter  = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public void Emit(LogEvent logEvent)
        {
            //logEvent.RenderMessage()

            using (var s = new StringWriter())
            {
                _formatter.Format(logEvent, s);

                _write(s.ToString());
            }
        }
    }
}
