using System;

namespace HappyLogging
{
	public sealed class LogEventDetails
	{
		public LogEventDetails(LogLevel logLevel, DateTime logDate, int managedThreadId, string content, Exception optionalException)
			: this(logLevel, logDate, managedThreadId, optionalException)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			Content = content;
			ContentGenerator = null;
		}
		public LogEventDetails(LogLevel logLevel, DateTime logDate, int managedThreadId, Func<string> contentGenerator, Exception optionalException)
			: this(logLevel, logDate, managedThreadId, optionalException)
		{
			if (contentGenerator == null)
				throw new ArgumentNullException(nameof(contentGenerator));
			Content = null;
			ContentGenerator = contentGenerator;
		}
		private LogEventDetails(LogLevel logLevel, DateTime logDate, int managedThreadId, Exception optionalException)
		{
			// Note: Explicitly check for all valid values rather than using Enum.IsDefined since IsDefined uses reflection and logging should be as cheap as possible (so
			// reflection is best avoided)
			if ((logLevel != LogLevel.Debug) && (logLevel != LogLevel.Info) && (logLevel != LogLevel.Warning) && (logLevel != LogLevel.Error))
				throw new ArgumentOutOfRangeException(nameof(logLevel));
			LogLevel = logLevel;
			LogDate = logDate;
			ManagedThreadId = managedThreadId;
			OptionalException = optionalException;
		}

		public LogLevel LogLevel { get; }

		public DateTime LogDate { get; }

		public int ManagedThreadId { get; }

		/// <summary>
		/// Precisely one of Content and ContentGenerator will be non-null (ContentGenerator should be used in cases where the evaluation of the log message is non-trivial,
		/// otherwise its value should be determined and the overhead of a Func allocation be avoided)
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Precisely one of Content and ContentGenerator will be non-null (ContentGenerator should be used in cases where the evaluation of the log message is non-trivial,
		/// otherwise its value should be determined and the overhead of a Func allocation be avoided)
		/// </summary>
		public Func<string> ContentGenerator { get; }

		/// <summary>
		/// This may be null as it is optional information (there is no guarantee that it will be non-null even if the LogLevel is Error)
		/// </summary>
		public Exception OptionalException { get; }
	}
}
