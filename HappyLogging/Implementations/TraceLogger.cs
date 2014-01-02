using System;
using System.Diagnostics;

namespace HappyLogging.Implementations
{
    /// <summary>
    /// Write log messages to Trace - this may be useful in production as configuration of the Trace symbol and of Trace Listeners can make the cost
    /// of individual writes quite cheap if no-one is looking for the information being output. If there are any log messages whose ContentGenerator
    /// calls are expensive then this is not side-stepped here, the ContentGenerators will be executed even if no-one if paying attention to the
    /// Trace, as such it might be a good policy to limit expensive log messages to Debug level and only show Info, Warning and Error messages
    /// on the Trace (the ErrorWithBackTrackLogger could be used to write these expensive messages - either to Trace or to a FileLogger -
    /// only in cases where an Error is recorded, where the expensive Debug messages may aid with investigation).
    /// </summary>
    public class TraceLogger : TextLogger
    {
        public TraceLogger(Func<LogEventDetails, string> messageFormatter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
            : base(messageFormatter, content => Trace.WriteLine(content), individualLogEntryErrorBehaviour) { }

        public TraceLogger() : this(Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour) { }
    }
}
