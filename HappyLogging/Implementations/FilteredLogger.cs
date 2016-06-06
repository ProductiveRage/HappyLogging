using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyLogging.Implementations
{
	/// <summary>
	/// Write log messages to trace, including additional content such as date, time and thread id
	/// </summary>
	public sealed class FilteredLogger : ILogEvents
	{
		private readonly ILogEvents _logger;
		public FilteredLogger(ILogEvents logger, IEnumerable<LogLevel> allowedLogLevels, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (allowedLogLevels == null)
				throw new ArgumentNullException(nameof(allowedLogLevels));
			if (!Enum.IsDefined(typeof(ErrorBehaviourOptions), individualLogEntryErrorBehaviour))
				throw new ArgumentOutOfRangeException("individualLogEntryErrorBehaviour");

			IndividualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;

			_logger = logger;
			AllowedLogLevels = allowedLogLevels.ToList().AsReadOnly();
			if (AllowedLogLevels.Any(l => !Enum.IsDefined(typeof(LogLevel), l)))
					throw new ArgumentException("Invalid LogLevel value specified");
			}
		public FilteredLogger(ILogEvents logger, IEnumerable<LogLevel> allowedLogLevels) : this(logger, allowedLogLevels, Defaults.IndividualLogEntryErrorBehaviour) { }
		public FilteredLogger(ILogEvents logger, params LogLevel[] allowedLogLevels) : this(logger, (IEnumerable<LogLevel>)allowedLogLevels) { }

		public static class Defaults
		{
			public static ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get { return ErrorBehaviourOptions.Ignore; } }
		}

		public ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get; }

		public IEnumerable<LogLevel> AllowedLogLevels { get; }

		/// <summary>
		/// This should throw an exception for a null message set but whether exceptions are thrown due to any other issues (eg. a message whose ContentGenerator
		/// delegate throws an exception or IO exceptions where file-writing is attempted) will vary depending upon the implementation
		/// </summary>
		public void Log(LogEventDetails message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!AllowedLogLevels.Contains(message.LogLevel))
				return;

			_logger.Log(message);
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

			_logger.Log(messages.Where(message =>
			{
				if ((message == null) && (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException))
					throw new ArgumentException("Null reference encountered in messages set");
				return AllowedLogLevels.Contains(message.LogLevel);
			}));
		}
	}
}
