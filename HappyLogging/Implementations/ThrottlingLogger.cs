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
		private DateTime? _lastFlushedAt;
		private int _flushInProgressIndicator;
		public ThrottlingLogger(
			ILogEvents logger,
			TimeSpan mimimumFrequency,
			int maximumNumberOfBufferedItems,
			MessageEvaluationBehaviourOptions messageEvaluationBehaviour,
			ErrorBehaviourOptions individualLogEntryErrorBehaviour)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");
			if (mimimumFrequency.Ticks <= 0)
				throw new ArgumentOutOfRangeException("mimimumFrequency", "must be a positive duration");
			if (maximumNumberOfBufferedItems <= 0)
				throw new ArgumentOutOfRangeException("maximumNumberOfBufferedItems", "must be a positive value");
			if (!Enum.IsDefined(typeof(MessageEvaluationBehaviourOptions), messageEvaluationBehaviour))
				throw new ArgumentOutOfRangeException("messageEvaluationBehaviour");
			if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLogEntryErrorBehaviour))
				throw new ArgumentOutOfRangeException("individualLogEntryErrorBehaviour");

			MaximumNumberOfBufferedItems = maximumNumberOfBufferedItems;
			MinimumFrequency = mimimumFrequency;
			MessageEvaluationBehaviour = messageEvaluationBehaviour;
			IndividualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;

			_logger = logger;
			_messages = new ConcurrentQueue<LogEventDetails>();
			_timer = new PauseableTimer(
				mimimumFrequency,
				FlushQueueIfNotAlreadyDoingSo
			);
			_lastFlushedAt = null;
			_flushInProgressIndicator = 0;
		}
		public ThrottlingLogger(ILogEvents logger) : this(
			logger,
			Defaults.MimimumFrequency,
			Defaults.MaximumNumberOfBufferedItems,
			Defaults.MessageEvaluationBehaviour,
			Defaults.IndividualLogEntryErrorBehaviour) { }

		public static class Defaults
		{
			public static TimeSpan MimimumFrequency { get { return TimeSpan.FromSeconds(2); } }
			public static int MaximumNumberOfBufferedItems { get { return 50; } }
			public static MessageEvaluationBehaviourOptions MessageEvaluationBehaviour { get { return MessageEvaluationBehaviourOptions.EvaluateWhenQueued; } }
			public static ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get { return ErrorBehaviourOptions.Ignore; } }
		}

		public enum MessageEvaluationBehaviourOptions
		{
			/// <summary>
			/// The message's content will only be evaluated when it is written. This guarantees that the evaluation will only happen when the log is definitely
			/// being written and it means that the work will be performed on a thread other than the caller's, which puts less load on the caller. However, if
			/// the content refers to something that is mutated between the message's creation and its being written (or to something that is disposed) then
			/// the message evaulation may be inaccurate or fail entirely.
			/// </summary>
			EvaluateWhenLogged,
			
			/// <summary>
			/// The message will be replaced by an instance where the content has been evaluated, to avoid potential concurrency issues with it being evaluated
			/// at a later time. This should be considered the default option in the interests of safety, the benefit of lazy evaluation may still be enjoyed
			/// if this logger implementation is behind a filtered logger.
			/// </summary>
			EvaluateWhenQueued
		}

		/// <summary>
		/// This will always be greater than zero
		/// </summary>
		public int MaximumNumberOfBufferedItems { get; }

		/// <summary>
		/// This will always be a positive duration
		/// </summary>
		public TimeSpan MinimumFrequency { get; }

		public MessageEvaluationBehaviourOptions MessageEvaluationBehaviour { get; }
		
		public ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get; }

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

				// If the message is to be evaluated when it's actually logged (which may be in the future), as opposed to when it's queued (which is right now)
				// the push it straight onto the queue. Or, if the content is a string and not a lazily-evaluating Func then we can also queue it immediately.
				if ((MessageEvaluationBehaviour == MessageEvaluationBehaviourOptions.EvaluateWhenLogged) || (message.ContentGenerator == null))
				{
					_messages.Enqueue(message);
					continue;
				}

				// If the message is to be evaluated when queued (ie. right now) then we need to evaulate the ContentGenerator. This will be desirable if there
				// is any content that is time dependent (eg. "time to complete = {x}ms") or in cases where there are any references that are required by the
				// message evaluation that might be disposed of between now and when the message is recorded. Note: Just because the message content is being
				// evaluated immediately doesn't negate the benefit of a content generator delegate, there could be relative-expensive-to-log messages that
				// should only be written away in debug mode, which case a FilteredLogger might wrap a ThrottlingLogger instance so that the messages are only
				// evaluated if Debug-level messages are allowed through the filter.
				string messageContents;
				if (message.Content != null)
					messageContents = message.Content;
				else
				{
					try
					{
						messageContents = message.ContentGenerator();
					}
					catch
					{
						if (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException)
							throw;
						continue;
					}
				}
				_messages.Enqueue(
					new LogEventDetails(
						message.LogLevel,
						message.LogDate,
						message.ManagedThreadId,
						() => messageContents,
						message.OptionalException
					)
				);
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
			public TimeSpan Frequency { get; }

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
