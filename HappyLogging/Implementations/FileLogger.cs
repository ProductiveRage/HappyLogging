using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HappyLogging.Implementations
{
    /// <summary>
    /// Append log messages to a file
    /// </summary>
    public class FileLogger : ILogEvents
    {
        private readonly Func<FileInfo> _fileRetriever;
        private readonly Func<LogEventDetails, string> _messageFormatter;
        public FileLogger(Func<FileInfo> fileRetriever, Func<LogEventDetails, string> messageFormatter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
        {
            if (fileRetriever == null)
                throw new ArgumentNullException("fileRetriever");
            if (messageFormatter == null)
                throw new ArgumentNullException("messageFormatter");
            if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLogEntryErrorBehaviour))
                throw new ArgumentOutOfRangeException("individualLogEntryErrorBehaviour");

            IndividualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;

            _fileRetriever = fileRetriever;
            _messageFormatter = messageFormatter;
        }
        public FileLogger(FileInfo file, Func<LogEventDetails, string> messageFormatter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
            : this(() => file, messageFormatter, individualLogEntryErrorBehaviour)
        {
            if (file == null)
                throw new ArgumentNullException("file");
        }
        public FileLogger(Func<FileInfo> fileRetriever) : this(fileRetriever, Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour) { }
        public FileLogger(FileInfo file) : this(file, Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour) { }

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
            if (combinedContentBuilder.Length == 0)
                return;

            var file = _fileRetriever();
            if (file == null)
                throw new Exception("fileRetriever returned null FileInfo reference");
            using (var writer = file.AppendText())
            {
                writer.WriteLine(combinedContentBuilder.ToString());
            }
        }
    }
}
