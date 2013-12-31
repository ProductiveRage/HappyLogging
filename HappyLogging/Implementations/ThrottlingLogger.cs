using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace HappyLogging.Implementations
{
    /// <summary>
    /// This will batch up log requests and dispatch them in groups, reducing the number of calls overall to the wrapped logger. This can be advantageous if the wrapped
    /// logger writes to disk since it means that the file will potentially be opened and written to less frequently. The throttling mechanism means that the log requests
    /// will only ever be written from a single thread at a time (though it's not guaranteed that it will be the same thread each time), so if all requests share a logger
    /// instance that utilises this class then all logging can be guaranteed to be single-threaded. This may also be advantageous for file loggers since it can prevent
    /// file-locking issues by preventing multiple threads from trying to write to the disk simultaneously.
    /// </summary>
    public class ThrottlingLogger : ILogEvents
    {
        private readonly ILogEvents _logger;
        private readonly ConcurrentQueue<LogEventDetails> _messages;
        private readonly PauseableTimer _timer;
        private readonly ErrorBehaviourOptions _individualLogEntryErrorBehaviour;
        private DateTime? _lastFlushedAt;
        private int _flushInProgressIndicator;
        public ThrottlingLogger(ILogEvents logger, TimeSpan mimimumFrequency, int maximumNumberOfBufferedItems, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (mimimumFrequency.Ticks <= 0)
                throw new ArgumentOutOfRangeException("mimimumFrequency", "must be a positive duration");
            if (maximumNumberOfBufferedItems <= 0)
                throw new ArgumentOutOfRangeException("maximumNumberOfBufferedItems", "must be a positive value");
            if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLogEntryErrorBehaviour))
                throw new ArgumentOutOfRangeException("individualLogEntryErrorBehaviour");

            MaximumNumberOfBufferedItems = maximumNumberOfBufferedItems;
            MinimumFrequency = mimimumFrequency;
            IndividualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;

            _logger = logger;
            _messages = new ConcurrentQueue<LogEventDetails>();
            _timer = new PauseableTimer(
                mimimumFrequency,
                FlushQueueIfNotAlreadyDoingSo
            );
            _individualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;
            _lastFlushedAt = null;
            _flushInProgressIndicator = 0;
        }
        public ThrottlingLogger(ILogEvents logger) : this(logger, Defaults.MimimumFrequency, Defaults.MaximumNumberOfBufferedItems, Defaults.IndividualLogEntryErrorBehaviour) { }

        public static class Defaults
        {
            public static TimeSpan MimimumFrequency { get { return TimeSpan.FromSeconds(2); } }
            public static int MaximumNumberOfBufferedItems { get { return 50; } }
            public static ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get { return ErrorBehaviourOptions.Ignore; } }
        }

        /// <summary>
        /// This will always be greater than zero
        /// </summary>
        public int MaximumNumberOfBufferedItems { get; private set; }

        /// <summary>
        /// This will always be a positive duration
        /// </summary>
        public TimeSpan MinimumFrequency { get; private set; }

        public ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get; private set; }

        /// <summary>
        /// This will initiate a flush if one is not already taking place. If one is initiated then the timer will be stopped until the process has completed,
        /// at which point it will be restarted.
        /// </summary>
        private void FlushQueueIfNotAlreadyDoingSo()
        {
            // If a queue flush is already in progress then do nothing
            // - If CompareExchange returns zero then it means that the value of _flushInProgressIndicator was zero before the call to CompareExchange (and so
            //   it will have then been set to one since the second argument is the value to set the first argument's reference to if it currently matches the
            //   third argument). If it doesn't return zero then _flushInProgressIndicator did not equal zero and so a flush is already in progress.
            if (Interlocked.CompareExchange(ref _flushInProgressIndicator, 1, 0) != 0)
                return;

            _timer.Stop();

            var messages = new List<LogEventDetails>();
            LogEventDetails message;
            while (_messages.TryDequeue(out message))
                messages.Add(message);
            if (messages.Count > 0)
            {
                // We have to swallow exceptions here to prevent leaving the timer's thread in an error state and from halting the logger completely. This is
                // a disadvantage to disconnecting the raising of log entries and their actual recording. If it's critical that failed log messages be tracked
                // somewhere, then the wrapped logger must incorporate that functionality.
                try { _logger.Log(messages); }
                catch { }
            }

            // Reset _flushInProgressIndicator to zero to indicate that the flush process has completed
            Interlocked.Exchange(ref _flushInProgressIndicator, 0);

            _lastFlushedAt = DateTime.Now;
        }

        /// <summary>
        /// This will throw an exception if issues are encountered - this includes cases of a null messages set or one containing any null references
        /// </summary>
        public void Log(IEnumerable<LogEventDetails> messages)
        {
            if (messages == null)
                throw new ArgumentNullException("message");

            foreach (var message in messages)
            {
                if ((message == null) && (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException))
                    throw new ArgumentException("Null reference encountered in messages set");
                _messages.Enqueue(message);
            }
            if (_messages.Count > MaximumNumberOfBufferedItems)
                FlushQueueIfNotAlreadyDoingSo();
            else if ((_lastFlushedAt != null) && (DateTime.Now > _lastFlushedAt.Value.Add(_timer.Frequency)))
                FlushQueueIfNotAlreadyDoingSo();
            else
                _timer.Start();
        }

        private class PauseableTimer
        {
            private readonly Timer _timer;
            public PauseableTimer(TimeSpan frequency, Action callback)
            {
                if (frequency.Ticks <= 0)
                    throw new ArgumentOutOfRangeException("mimimumFrequency", "must be a positive duration");
                if (callback == null)
                    throw new ArgumentNullException("callback");

                Frequency = frequency;
                _timer = new Timer(
                    state =>
                    {
                        try { callback(); }
                        catch { }
                    }
                );
                Stop(); // Explicitly initialise timer in stopped state
            }

            /// <summary>
            /// This will always be a positive duration
            /// </summary>
            public TimeSpan Frequency { get; private set; }

            public void Start()
            {
                _timer.Change(Frequency, Frequency);
            }

            public void Stop()
            {
                _timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
            }
        }
    }
}
