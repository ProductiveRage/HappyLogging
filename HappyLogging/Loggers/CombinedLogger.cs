using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyLogging.Loggers
{
    /// <summary>
    /// Write log messages to multiple loggers
    /// </summary>
    public class CombinedLogger : ILogEvents
    {
        public CombinedLogger(IEnumerable<ILogEvents> loggers, ErrorBehaviourOptions individualLoggerErrorBehaviour)
        {
            if (loggers == null)
                throw new ArgumentNullException("loggers");
            if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLoggerErrorBehaviour))
                throw new ArgumentOutOfRangeException("individualLoggerErrorBehaviour");

            Loggers = loggers.ToList().AsReadOnly();
            if (Loggers.Any(logger => logger == null))
                throw new ArgumentException("Null reference encountered in loggers set");
            IndividualLoggerErrorBehaviour = individualLoggerErrorBehaviour;
        }
        public CombinedLogger(IEnumerable<ILogEvents> loggers) : this(loggers, Defaults.IndividualLoggerErrorBehaviour) { }
        public CombinedLogger(params ILogEvents[] loggers) : this((IEnumerable<ILogEvents>)loggers) { }

        public static class Defaults
        {
            public static ErrorBehaviourOptions IndividualLoggerErrorBehaviour { get { return ErrorBehaviourOptions.Ignore; } }
        }

        /// <summary>
        /// This will never be null nor a set containing any nulls
        /// </summary>
        public IEnumerable<ILogEvents> Loggers { get; private set; }

        public ErrorBehaviourOptions IndividualLoggerErrorBehaviour { get; private set; }

        /// <summary>
        /// This should throw an exception for a null messages set but whether exceptions are thrown due to any other issues (eg. null references within the
        /// messages set, messages whose ContentGenerator delegates throw exception or IO exceptions where file-writing is attempted) will vary depending
        /// upon the implementation
        /// </summary>
        public void Log(IEnumerable<LogEventDetails> messages)
        {
            if (messages == null)
                throw new ArgumentNullException("messages");

            foreach (var logger in Loggers)
            {
                try
                {
                    logger.Log(messages);
                }
                catch
                {
                    if (IndividualLoggerErrorBehaviour == ErrorBehaviourOptions.ThrowException)
                        throw;
                }
            }
        }
    }
}
