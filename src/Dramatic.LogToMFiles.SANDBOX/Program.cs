using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dramatic.LogToMFiles.SANDBOX
{
    class Program
    {
        static void Main(string[] _)
        {
            try
            {
                var sampleVaultName     = "Serilog.Sinks.MFilesObject";

                var serverApp           = new MFilesAPI.MFilesServerApplication();
                serverApp.Connect(MFilesAPI.MFAuthType.MFAuthTypeLoggedOnWindowsUser);
                var vaultOnServer       = serverApp.GetOnlineVaults().GetVaultByName(sampleVaultName); // The "Serilog.Sinks.MFilesObject" demo vault that mysteriously bears the same name as the logging solution
                if (vaultOnServer == null) { throw new ArgumentException($"Cannot find sample vault \"{"Serilog.Sinks.MFilesObject"}\". Is it installed on this server?"); }

                var vault               = serverApp.LogInAsUserToVault(vaultOnServer.GUID);  // "{D449E438-89EE-42BB-9769-B862E9B1B140}"


                // Define vault structure for logging if it isn't there: OT "Log", CL "Log" and PD "LogMessage" and aliases to find them back.
                var structureConfig = new LoggingVaultStructureConfiguration
                {
                    // Structure for the LogObject sink
                    LogObjectTypeNameSingular   = DefaultLoggingVaultStructure.LogObjectTypeNameSingular,             // "Log"
                    LogObjectTypeNamePlural     = DefaultLoggingVaultStructure.LogObjectTypeNamePlural,               // "Logs"
                    LogMessagePropDefName       = DefaultLoggingVaultStructure.LogMessagePropDefName,                 // "LogMessage"

                    LogObjectTypeAlias          = DefaultLoggingVaultStructure.LogObjectTypeAlias,                    // "OT.Serilog.MFilesObjectLogSink.Log"
                    LogClassAlias               = DefaultLoggingVaultStructure.LogClassAlias,                         // "CL.Serilog.MFilesObjectLogSink.Log"
                    LogMessagePropDefAlias      = DefaultLoggingVaultStructure.LogMessagePropertyDefinitionAlias,     // "PD.Serilog.MFilesObjectLogSink.LogMessage"

                    // Structure for the LogFile sink
                    LogFileClassName            = DefaultLoggingVaultStructure.LogFileClassName,                      // "LogFile"
                    LogFileClassAlias           = DefaultLoggingVaultStructure.LogFileClassAlias                      // "CL.Serilog.MFilesObjectLogSink.LogFile"
                };


                // -----------------------------------------------------------------------------------------------------------------------------------------
                // Repo to write a message to a Log Object LogMessage property
                var rollingObjectRepo = new LogObjectRepository(vault, $"[{Environment.MachineName}] LogObject-", structureConfig.LogObjectTypeAlias, structureConfig.LogClassAlias, structureConfig.LogMessagePropDefAlias);
                rollingObjectRepo.SaveLogMessage($"Test message 2 {DateTime.Now:HH:mm:ss}");

                // -----------------------------------------------------------------------------------------------------------------------------------------
                // Repo to write a message to a Log File txt document object
                var rollingFileRepo = new LogFileRepository(vault, $"[{Environment.MachineName}] LogFile-", structureConfig.LogFileClassAlias);
                rollingFileRepo.SaveLogMessage($"Test message 2 {DateTime.Now:HH:mm:ss}");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToDetailedString());
            }

            Console.WriteLine("Hit enter to exit");
            Console.ReadLine();
        }
    }



    // source: https://gist.github.com/RehanSaeed/c256828acc04d685d024
    public static class ExceptionExtensions
    {
        public static string ToDetailedString(this Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return ToDetailedString(exception, ExceptionOptions.Default);
        }

        public static string ToDetailedString(this Exception exception, ExceptionOptions options)
        {
            var stringBuilder = new StringBuilder();

            AppendValue(stringBuilder, "Type", exception.GetType().FullName, options);

            foreach (PropertyInfo property in exception
                .GetType()
                .GetProperties()
                .OrderByDescending(x => string.Equals(x.Name, nameof(exception.Message), StringComparison.Ordinal))
                .ThenByDescending(x => string.Equals(x.Name, nameof(exception.Source), StringComparison.Ordinal))
                .ThenBy(x => string.Equals(x.Name, nameof(exception.InnerException), StringComparison.Ordinal))
                .ThenBy(x => string.Equals(x.Name, nameof(AggregateException.InnerExceptions), StringComparison.Ordinal)))
            {
                var value = property.GetValue(exception, null);
                if (value == null && options.OmitNullProperties)
                {
                    if (options.OmitNullProperties)
                    {
                        continue;
                    }
                    else
                    {
                        value = string.Empty;
                    }
                }

                AppendValue(stringBuilder, property.Name, value, options);
            }

            return stringBuilder.ToString().TrimEnd('\r', '\n');
        }

        private static void AppendCollection(
            StringBuilder stringBuilder,
            string propertyName,
            IEnumerable collection,
            ExceptionOptions options)
        {
            stringBuilder.AppendLine($"{options.Indent}{propertyName} =");

            var innerOptions = new ExceptionOptions(options, options.CurrentIndentLevel + 1);

            var i = 0;
            foreach (var item in collection)
            {
                var innerPropertyName = $"[{i}]";

                if (item is Exception)
                {
                    var innerException = (Exception)item;
                    AppendException(
                        stringBuilder,
                        innerPropertyName,
                        innerException,
                        innerOptions);
                }
                else
                {
                    AppendValue(
                        stringBuilder,
                        innerPropertyName,
                        item,
                        innerOptions);
                }

                ++i;
            }
        }

        private static void AppendException(
            StringBuilder stringBuilder,
            string propertyName,
            Exception exception,
            ExceptionOptions options)
        {
            var innerExceptionString = ToDetailedString(
                exception,
                new ExceptionOptions(options, options.CurrentIndentLevel + 1));

            stringBuilder.AppendLine($"{options.Indent}{propertyName} =");
            stringBuilder.AppendLine(innerExceptionString);
        }

        private static string IndentString(string value, ExceptionOptions options)
        {
            return value.Replace(Environment.NewLine, Environment.NewLine + options.Indent);
        }

        private static void AppendValue(
            StringBuilder stringBuilder,
            string propertyName,
            object value,
            ExceptionOptions options)
        {
            if (value is DictionaryEntry)
            {
                DictionaryEntry dictionaryEntry = (DictionaryEntry)value;
                stringBuilder.AppendLine($"{options.Indent}{propertyName} = {dictionaryEntry.Key} : {dictionaryEntry.Value}");
            }
            else if (value is Exception)
            {
                var innerException = (Exception)value;
                AppendException(
                    stringBuilder,
                    propertyName,
                    innerException,
                    options);
            }
            else if (value is IEnumerable && !(value is string))
            {
                var collection = (IEnumerable)value;
                if (collection.GetEnumerator().MoveNext())
                {
                    AppendCollection(
                        stringBuilder,
                        propertyName,
                        collection,
                        options);
                }
            }
            else
            {
                stringBuilder.AppendLine($"{options.Indent}{propertyName} = {value}");
            }
        }
    }

    public struct ExceptionOptions
    {
        public static readonly ExceptionOptions Default = new ExceptionOptions()
        {
            CurrentIndentLevel = 0,
            IndentSpaces = 4,
            OmitNullProperties = true
        };

        internal ExceptionOptions(ExceptionOptions options, int currentIndent)
        {
            this.CurrentIndentLevel = currentIndent;
            this.IndentSpaces = options.IndentSpaces;
            this.OmitNullProperties = options.OmitNullProperties;
        }

        internal string Indent { get { return new string(' ', this.IndentSpaces * this.CurrentIndentLevel); } }

        internal int CurrentIndentLevel { get; set; }

        public int IndentSpaces { get; set; }

        public bool OmitNullProperties { get; set; }
    }
}
