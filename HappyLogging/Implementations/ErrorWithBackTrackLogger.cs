using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyLogging.Implementations
{
    /// <summary>
    /// This will not pass most non-Error messages to the wrapped logger, but when an Error message is encountered then the most recent log messages for all levels will be
    /// sent to the logger along with the Error message. This means that detailed information can be logged with errors but not output most of the time. Historical messages
    /// will be passed through at most once - when they have been included with an error, they are dropped from the internal store. The size of the internal store is determined
    /// by the constructor parameter "maximumNumberOfHistoricalMessagesToMaintain". The history included with an error may contain only log entries with the same managed thread
    /// id as the error log message or it may contain ALL log entries in the history store. This logger may be particularly useful for scenarios where Debug-level log messages
    /// would not only take up a lot of space in disk logs but might also be expensive to generate - log messages could include serialised request and response messages, this
    /// filter would preventing the messages' ContentGenerator delegates from being executed except when they would be useful for tracking down errors (in which case the over-
    /// head of the serialisation would more likely be worth it).
    /// Warning: This class is not thread-safe. In order to be used in a multi-threaded environment it must be wrapped in a ThrottlingLogger (or some other mechanism) to ensure
    /// that it is never accessed by more than one thread at any time.
    /// </summary>
    public class ErrorWithBackTrackLogger : ILogEvents
    {
        private readonly ILogEvents _logger;
        private readonly Queue<LogEventDetails> _messages;
        public ErrorWithBackTrackLogger(
            ILogEvents logger,
            int maximumNumberOfHistoricalMessagesToMaintain,
            int maximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry,
            HistoryLoggingBehaviourOptions historyLoggingBehaviour,
            ErrorBehaviourOptions individualLogEntryErrorBehaviour)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (maximumNumberOfHistoricalMessagesToMaintain <= 0)
                throw new ArgumentOutOfRangeException("maximumNumberOfHistoricalMessagesToMaintain", "must be greater than zero");
            if (maximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry <= 0)
                throw new ArgumentOutOfRangeException("maximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry", "must be greater than zero");
            if (!Enum.IsDefined(typeof(HistoryLoggingBehaviourOptions), historyLoggingBehaviour))
                throw new ArgumentOutOfRangeException("historyLoggingBehaviour");
            if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLogEntryErrorBehaviour))
                throw new ArgumentOutOfRangeException("individualLogEntryErrorBehaviour");

            MaximumNumberOfHistoricalMessagesToMaintain = maximumNumberOfHistoricalMessagesToMaintain;
            MaximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry = maximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry;
            HistoryLoggingBehaviour = historyLoggingBehaviour;
            IndividualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;
            
            _logger = logger;
            _messages = new Queue<LogEventDetails>(maximumNumberOfHistoricalMessagesToMaintain);
        }
        public ErrorWithBackTrackLogger(ILogEvents logger) : this(
            logger,
            Defaults.MaximumNumberOfHistoricalMessagesToMaintain,
            Defaults.MaximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry,
            Defaults.HistoryLoggingBehaviourOptions,
            Defaults.IndividualLogEntryErrorBehaviour) { }

        public static class Defaults
        {
            public static int MaximumNumberOfHistoricalMessagesToMaintain { get { return 1000; } }
            public static int MaximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry { get { return 100; } }
            public static HistoryLoggingBehaviourOptions HistoryLoggingBehaviourOptions { get { return HistoryLoggingBehaviourOptions.IncludePrecedingMessagesFromTheSameThreadOnly; } }
            public static ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get { return ErrorBehaviourOptions.Ignore; } }
        }

        public enum HistoryLoggingBehaviourOptions
        {
            IncludeAllPrecedingMessages,
            IncludePrecedingMessagesFromTheSameThreadOnly
        }

        /// <summary>
        /// This is the total number of log entries that will be maintained in memory to be logged with any errors. If HistoryLoggingBehaviour is set to
        /// IncludePrecedingMessagesFromTheSameThreadOnly then this would likely be greater than MaximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry
        /// so that it is large enough to contain at least some history about all executing requests. This will always be greater than zero.
        /// </summary>
        public int MaximumNumberOfHistoricalMessagesToMaintain { get; private set; }

        /// <summary>
        /// This will always be greater than zero
        /// </summary>
        public int MaximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry { get; private set; }

        public HistoryLoggingBehaviourOptions HistoryLoggingBehaviour { get; private set; }

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

            var messagesToPassThrough = new List<LogEventDetails>();
            foreach (var message in messages)
            {
                if ((message == null) && (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException))
                    throw new ArgumentException("Null reference encountered in messages set");

                if (message.LogLevel == LogLevel.Error)
                {
                    var historicalMessagesToIncludeWithError = HistoryLoggingBehaviour == HistoryLoggingBehaviourOptions.IncludeAllPrecedingMessages
                        ? (IEnumerable<LogEventDetails>)_messages
                        :_messages.Where(m => m.ManagedThreadId == message.ManagedThreadId);
                    if (historicalMessagesToIncludeWithError.Any())
                    {
                        var historicalMessagesToIncludeWithErrorArray = historicalMessagesToIncludeWithError.ToArray();
                        if (historicalMessagesToIncludeWithErrorArray.Length > MaximumNumberOfHistoricalMessagesToMaintain)
                        {
                            messagesToPassThrough.AddRange(
                                historicalMessagesToIncludeWithErrorArray.Skip(
                                    MaximumNumberOfHistoricalMessagesToIncludeWithAnErrorEntry - historicalMessagesToIncludeWithErrorArray.Length
                                )
                            );
                        }
                        else
                            messagesToPassThrough.AddRange(historicalMessagesToIncludeWithErrorArray);

                        var historicalMessagesLookup = new HashSet<LogEventDetails>(historicalMessagesToIncludeWithError);
                        var messagesToKeep = _messages.Where(m => !historicalMessagesLookup.Contains(m)).ToArray();
                        _messages.Clear();
                        foreach (var messageToKeep in messagesToKeep)
                            _messages.Enqueue(messageToKeep);
                    }
                    
                    messagesToPassThrough.Add(message);
                }
                else
                {
                    _messages.Enqueue(message);
                    while (_messages.Count >= MaximumNumberOfHistoricalMessagesToMaintain)
                        _messages.Dequeue();
                }
            }

            if (messagesToPassThrough.Any())
                _logger.Log(messagesToPassThrough);
        }
    }
}
