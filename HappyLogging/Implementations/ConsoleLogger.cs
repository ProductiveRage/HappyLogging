using System;
using System.Collections.Generic;
using System.Text;

namespace HappyLogging.Implementations
{
    /// <summary>
    /// Write log messages to the Console
    /// </summary>
    public class ConsoleLogger : ILogEvents
    {
        private readonly Func<LogEventDetails, string> _messageFormatter;
        public ConsoleLogger(Func<LogEventDetails, string> messageFormatter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
        {
            if (messageFormatter == null)
                throw new ArgumentNullException("messageFormatter");
            if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLogEntryErrorBehaviour))
                throw new ArgumentOutOfRangeException("individualLogEntryErrorBehaviour");

            IndividualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;
            _messageFormatter = messageFormatter;
        }
        public ConsoleLogger() : this(Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour) { }

        public static class Defaults
        {
            public static Func<LogEventDetails, string> MessageFormatter { get { return MessageFormatting.DefaultMessageFormatter.Format; } }
            public static ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get { return ErrorBehaviourOptions.Ignore; } }
        }

        public ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get; private set; }

        /// <summary>
        /// This should throw an exception for a null messages set but whether exceptions are thrown due to any other issues (eg. null references within the
        /// messages set, messages whose ContentGenerator delegates throw exception or IO exceptions where file-writing is attempted) will vary depending
        /// upon the implementation
        /// </summary>
        public void Log(IEnumerable<LogEventDetails> messages)
        {
            if (messages == null)
                throw new ArgumentNullException("messages");

            var combinedContentBuilder = new StringBuilder();
            foreach (var message in messages)
            {
                if ((message == null) && (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException))
                    throw new ArgumentException("Null reference encountered in messages set");

                string messageContentToDisplay;
                try
                {
                    messageContentToDisplay = _messageFormatter(message);
                    if (messageContentToDisplay == null)
                        throw new Exception("messageFormatter returned null");
                }
                catch
                {
                    if (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException)
                        throw;
                    continue;
                }
                combinedContentBuilder.AppendLine(messageContentToDisplay);
            }
            if (combinedContentBuilder.Length > 0)
                Console.WriteLine(combinedContentBuilder.ToString());
        }
    }
}
