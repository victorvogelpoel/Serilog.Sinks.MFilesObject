using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.MFilesObject
{
    public class ControlledLevelSwitch
    {
        // If non-null, then background level checks will be performed; set either through the constructor
        // or in response to a level specification from the server. Never set to null after being made non-null.
        LoggingLevelSwitch  _controlledSwitch;
        LogEventLevel?      _originalLevel;

        public ControlledLevelSwitch(LoggingLevelSwitch controlledSwitch = null)
        {
            _controlledSwitch = controlledSwitch;
        }

        public bool IsActive => _controlledSwitch != null;

        public bool IsIncluded(LogEvent evt)
        {
            // Concurrent, but not synchronized.
            var controlledSwitch = _controlledSwitch;

            return controlledSwitch == null || (int)controlledSwitch.MinimumLevel <= (int)evt.Level;
        }

        public void Update(LogEventLevel? minimumAcceptedLevel)
        {
            if (minimumAcceptedLevel == null)
            {
                if (_controlledSwitch != null && _originalLevel.HasValue)
                {
                    _controlledSwitch.MinimumLevel = _originalLevel.Value;
                }

                return;
            }

            if (_controlledSwitch == null)
            {
                // The server is controlling the logging level, but not the overall logger. Hence, if the server
                // stops controlling the level, the switch should become transparent.
                _originalLevel      = LevelAlias.Minimum;
                _controlledSwitch   = new LoggingLevelSwitch(minimumAcceptedLevel.Value);

                return;
            }

            if (!_originalLevel.HasValue)
            {
                _originalLevel = _controlledSwitch.MinimumLevel;
            }

            _controlledSwitch.MinimumLevel = minimumAcceptedLevel.Value;
        }
    }
}
