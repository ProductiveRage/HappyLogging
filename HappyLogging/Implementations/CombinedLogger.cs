using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyLogging.Implementations
{
	/// <summary>
	/// Write log messages to multiple loggers
	/// </summary>
	public sealed class CombinedLogger : ILogEvents
	{
		public CombinedLogger(IEnumerable<ILogEvents> loggers, ErrorBehaviourOptions individualLoggerErrorBehaviour)
		{
			if (loggers == null)
				throw new ArgumentNullException(nameof(loggers));
			if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLoggerErrorBehaviour))
				throw new ArgumentOutOfRangeException(nameof(individualLoggerErrorBehaviour));

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
		public IEnumerable<ILogEvents> Loggers { get; }

		public ErrorBehaviourOptions IndividualLoggerErrorBehaviour { get; }

		/// <summary>
		/// This should throw an exception for a null message set but whether exceptions are thrown due to any other issues (eg. a message whose ContentGenerator
		/// delegate throws an exception or IO exceptions where file-writing is attempted) will vary depending upon the implementation
		/// </summary>
		public void Log(LogEventDetails message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			foreach (var logger in Loggers)
			{
				try
				{
					logger.Log(message);
				}
				catch
				{
					if (IndividualLoggerErrorBehaviour == ErrorBehaviourOptions.ThrowException)
						throw;
				}
			}
		}

		/// <summary>
		/// This should throw an exception for a null messages set but whether exceptions are thrown due to any other issues (eg. null references within the
		/// messages set, messages whose ContentGenerator delegates throw exception or IO exceptions where file-writing is attempted) will vary depending
		/// upon the implementation
		/// </summary>
		public void Log(IEnumerable<LogEventDetails> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

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
