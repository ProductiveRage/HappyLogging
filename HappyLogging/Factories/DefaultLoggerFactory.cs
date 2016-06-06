using System;
using System.IO;

namespace HappyLogging
{
	/// <summary>
	/// These loggers demonstrate some of the functionality of the library. They buffer up messages before writing to disk but send them to the Trace in
	/// real time. All of these loggers use the ThrottlingLogger class and so are intended to operate with a single logger instance shared across all
	/// requests.
	/// </summary>
	public static class DefaultLoggerFactory
	{
		/// <summary>
		/// This will log all messages to disk with a filename that appends today's date to between the file name and extension of the specified FileInfo.
		/// Messages with Info, Warning and Error levels are written to the Trace in real time (though they are buffered up to cut down on file IO).
		/// </summary>
		public static ILogEvents DailyLogger(FileInfo fileBase)
		{
			if (fileBase == null)
				throw new ArgumentNullException(nameof(fileBase));

			return CombineWithTraceLogger(
				DailyLogger(fileBase, null, LogLevelFilteringOptions.IncludeAllMessages)
			);
		}

		/// <summary>
		/// This will log all messages to disk with a filename that appends today's date to between the file name and extension of the specified FileInfo.
		/// If that file exceeds the specified targetMaximumFileSizeInBytes then subsequent files will be created that append an index after the date.
		/// Messages with Info, Warning and Error levels are written to the Trace in real time (though they are buffered up to cut down on file IO).
		/// </summary>
		public static ILogEvents DailyLogger(FileInfo fileBase, int targetMaximumFileSizeInBytes)
		{
			if (fileBase == null)
				throw new ArgumentNullException(nameof(fileBase));
			if (targetMaximumFileSizeInBytes <= 0)
				throw new ArgumentOutOfRangeException(nameof(targetMaximumFileSizeInBytes));

			return CombineWithTraceLogger(
				DailyLogger(fileBase, targetMaximumFileSizeInBytes, LogLevelFilteringOptions.IncludeAllMessages)
			);
		}

		/// <summary>
		/// This will not log anything to disk until a message with Error log level is received, at which point it will write all recent messages that
		/// were recorded on the same thread before the Error message is written. The hope being that these messages will offer helpful context for the
		/// error without detailed logs having to be written at all times. The log will be written to disk with a filename that appends today's date to
		/// between the file name and extension of the specified FileInfo. Messages with Info, Warning and Error levels are written to the Trace in real
		/// time (though they are buffered up to cut down on file IO).
		/// </summary>
		public static ILogEvents DailyErrorLogger(FileInfo fileBase)
		{
			if (fileBase == null)
				throw new ArgumentNullException(nameof(fileBase));

			return CombineWithTraceLogger(
				DailyLogger(fileBase, null, LogLevelFilteringOptions.ErrorOnly)
			);
		}

		private static ILogEvents DailyLogger(FileInfo fileBase, int? targetMaximumFileSizeInBytes, LogLevelFilteringOptions logLevelFilteringOptions)
		{
			if (fileBase == null)
				throw new ArgumentNullException(nameof(fileBase));
			if ((targetMaximumFileSizeInBytes != null) && (targetMaximumFileSizeInBytes.Value <= 0))
				throw new ArgumentOutOfRangeException(nameof(targetMaximumFileSizeInBytes));
			if ((logLevelFilteringOptions != LogLevelFilteringOptions.ErrorOnly) && (logLevelFilteringOptions != LogLevelFilteringOptions.IncludeAllMessages))
				throw new ArgumentOutOfRangeException(nameof(logLevelFilteringOptions));

			var extension = fileBase.Extension; // Note: This will include the dot (eg. ".txt")
			var filenameWithoutExtension = fileBase.FullName.Substring(0, fileBase.FullName.Length - extension.Length);
			Func<FileInfo> fileRetriever = () =>
			{
				var fileIndex = 0;
				while (true)
				{
					var targetFile = new FileInfo(
						string.Format(
							"{0} {1:yyyy-MM-dd}{2}{3}",
							filenameWithoutExtension,
							DateTime.Now,
							(fileIndex == 0) ? "" : string.Format(".{0}", fileIndex),
							extension
						)
					);
					if ((targetMaximumFileSizeInBytes == null) || !targetFile.Exists || (targetFile.Length < targetMaximumFileSizeInBytes.Value))
						return targetFile;
					fileIndex++;
				}
			};

			// The default behaviour of the FileLogger (which we aren't overriding here, the default will be fine) is to format messages using the
			// DefaultMessageFormatter class and to ignore any individual log message error (the ContentGenerator delegate for each message is an
			// abitrary code execution and so could potentially error)
			ILogEvents logger = new FileLogger(fileRetriever);

			// The default ErrorWithBackTrackLogger is to only record messages that were written by the same thread as that which recorded the error.
			// It will maintain an internal history of 1000 items and include no more than 100 with any error log message. If there are a high number
			// of simultaneous requests expected or the request durations are highly variable then the queue size of 1000 may not be sufficient but
			// it should probably be fine as a starting point.
			if (logLevelFilteringOptions == LogLevelFilteringOptions.ErrorOnly)
				logger = new ErrorWithBackTrackLogger(logger);
 
			// The ThrottlingLogger's default behaviour is to queue up no more than 50 messages but to otherwise log no more than every two seconds.
			// This compromise works well if a targetMaximumFileSizeInBytes is specified as it means that the file should not exceed the target
			// maximum by too much but the IO overhead of identifying the correct file to write is not done for each log request. If the current
			// file is just below the targetMaximumFileSizeInBytes and an error message is recorded with its historical data (only applicable
			// if logLevelFilteringOptions is set to ErrorOnly) then the file will likely exceed the maximum; that's why it is considered a
			// "target maximum" rather than a strict cap.
			return new ThrottlingLogger(logger);
		}

		private enum LogLevelFilteringOptions
		{
			ErrorOnly,
			IncludeAllMessages
		}

		private static ILogEvents CombineWithTraceLogger(ILogEvents logger)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			// Note: Here an AsyncLogger is used just so that the log writer needn't worry about waiting for any trace listeners to deal with the
			// messages being broadcast, they can just carry on with their real work and the listeners will get the data on a separate thread
			return new CombinedLogger(
				logger,
				new AsyncLogger(
					new FilteredLogger(
						new TraceLogger(),
						LogLevel.Info,
						LogLevel.Warning,
						LogLevel.Error
					)
				)
			);
		}
	}
}
