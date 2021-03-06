# Changelog

`Serilog.Sinks.MFilesObject` is a Serilog structured logging sink that uses the M-Files COM API to emit event messages to a "rolling" Log object or Log file in an M-Files vault.

*"M-Files is the intelligent document management system. With M-Files, organizations organize all their documents and information so that they can easily find, manage and secure them. M-Files is the smartest DMS you’ve ever seen."*

Use Serilog structured logging in your M-Files console, integration and vault application solutions and see the logging appear as an object in the vault. Just open the M-Files desktop app and inspect the logging of your application.

## v1.0.0

### Features

Use Serilog structured logging in your M-Files console, integration and vault application solutions with M-Files COM API and see the structured logging appear as a Log object or Log file in the vault. 

This release features two sinks:

* `MFilesLogObjectMessageSink`: Log to a M-Files Log object with a multi-line text property.
* `MFilesLogFileSink`: Log to an M-Files Log File.

Both sinks work with batching, where new log messages are appended every 10 seconds, to prevent overly stress on the vault.

#### Log object

The log object is 'rolling': the sinks creates a new Log object in the vault for the current day, eg "Log 2022-01-27". When the multi-line text property reaches its limit of 10000 characters, the sink creates a new Log object for today with an ordinal between braces, eg "Log 2022-01-27 (2)".

#### Log file

The log file is 'rolling' as well, where the sink creates a Log text file document for the current day, eg "Log 2022-01-27.txt".

### Requirements

These sinks require a M-Files COM API installation, which is installed with the M-Files Desktop app.


<!--
## v1.0.0

### Features

### Improvements

### Fixes

### Other

### Breaking changes

-->