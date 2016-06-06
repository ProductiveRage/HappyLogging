using System;
using System.Threading;

namespace HappyLogging
{
	public static class ILogEvents_Extensions
	{
		public static void Log(this ILogEvents logger, LogLevel logLevel, DateTime logDate, int managedThreadId, Func<string> contentGenerator, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.Log(new LogEventDetails(logLevel, logDate, managedThreadId, contentGenerator, exception));
		}

		/// <summary>
		/// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
		/// of the logger implementation
		/// </summary>
		public static void LogIgnoringAnyError(this ILogEvents logger, LogLevel logLevel, DateTime logDate, int managedThreadId, Func<string> contentGenerator, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			try
			{
				logger.Log(new LogEventDetails(logLevel, logDate, managedThreadId, contentGenerator, exception));
			}
			catch { }
		}

		/// <summary>
		/// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
		/// of the logger implementation
		/// </summary>
		public static void LogIgnoringAnyError(this ILogEvents logger, LogLevel logLevel, Func<string> contentGenerator, Exception exception = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			try
			{
				logger.Log(new LogEventDetails(logLevel, DateTime.Now, Thread.CurrentThread.ManagedThreadId, contentGenerator, exception));
			}
			catch { }
		}

		public static void Log(this ILogEvents logger, LogLevel logLevel, DateTime logDate, int managedThreadId, string content, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.Log(new LogEventDetails(logLevel, logDate, managedThreadId, content, exception));
		}

		/// <summary>
		/// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
		/// of the logger implementation
		/// </summary>
		public static void LogIgnoringAnyError(this ILogEvents logger, LogLevel logLevel, DateTime logDate, int managedThreadId, string content, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			try
			{
				logger.Log(new LogEventDetails(logLevel, logDate, managedThreadId, content, exception));
			}
			catch { }
		}

		/// <summary>
		/// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
		/// of the logger implementation
		/// </summary>
		public static void LogIgnoringAnyError(this ILogEvents logger, LogLevel logLevel, string content, Exception exception = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			try
			{
				logger.Log(new LogEventDetails(logLevel, DateTime.Now, Thread.CurrentThread.ManagedThreadId, content, exception));
			}
			catch { }
		}

		/// <summary>
		/// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
		/// of the logger implementation.
		/// </summary>
		public static void LogIgnoringAnyError(this ILogEvents logger, Exception error)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			// If there's no error then there's nothing to log, but this method is not supposed to throw an error when operating against a logger
			// implementation (which is why the ArgumentNullException above is acceptable) so if error is null then do nothing
			if (error == null)
				return;

			try
			{
				logger.Log(new LogEventDetails(LogLevel.Error, DateTime.Now, Thread.CurrentThread.ManagedThreadId, "", error));
			}
			catch { }
		}
	}
}
