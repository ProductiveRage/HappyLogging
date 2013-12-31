using System;
using System.Threading;

namespace HappyLogging.Loggers
{
    public static class ILogEvents_Extensions
	{
        public static void Log(this ILogEvents logger, LogLevel logLevel, DateTime logDate, int managedThreadId, Func<string> contentGenerator, Exception exception)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");

            logger.Log(new[] { new LogEventDetails(logLevel, logDate, managedThreadId, contentGenerator, exception) });
        }

        /// <summary>
        /// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
        /// of the logger implementation
        /// </summary>
        public static void LogIgnoringAnyError(this ILogEvents logger, LogLevel logLevel, DateTime logDate, int managedThreadId, Func<string> contentGenerator, Exception exception)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");

            try
            {
                logger.Log(logLevel, logDate, managedThreadId, contentGenerator, exception);
            }
            catch { }
        }

        /// <summary>
        /// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
        /// of the logger implementation
        /// </summary>
        public static void LogIgnoringAnyError(this ILogEvents logger, LogLevel logLevel, Func<string> contentGenerator, Exception exception)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");

            try
            {
                logger.Log(logLevel, DateTime.Now, Thread.CurrentThread.ManagedThreadId, contentGenerator, exception);
            }
            catch { }
        }

        /// <summary>
        /// Wrap logging request in a try..catch and swallow any exception - this is an extension method that guarantees the exception will be caught, regardless
        /// of the logger implementation
        /// </summary>
        public static void LogIgnoringAnyError(this ILogEvents logger, LogLevel logLevel, Func<string> contentGenerator)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");

            LogIgnoringAnyError(logger, logLevel, contentGenerator, null);
        }
	}
}
