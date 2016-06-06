using System;
using System.IO;

namespace HappyLogging
{
	/// <summary>
	/// Append log messages to a file
	/// </summary>
	public sealed class FileLogger : TextLogger
	{
		public FileLogger(
			Func<FileInfo> fileRetriever,
			Func<LogEventDetails, string> messageFormatter,
			ErrorBehaviourOptions individualLogEntryErrorBehaviour,
			bool tryToCreateFolderIfRequired,
			object optionalSyncLock)
			: base(messageFormatter, GetOutputWriter(null, fileRetriever, tryToCreateFolderIfRequired, optionalSyncLock), individualLogEntryErrorBehaviour) { }

		public FileLogger(
			FileInfo file,
			Func<LogEventDetails, string> messageFormatter,
			ErrorBehaviourOptions individualLogEntryErrorBehaviour,
			bool tryToCreateFolderIfRequired,
			object optionalSyncLock)
			: base(messageFormatter, GetOutputWriter(file, null, tryToCreateFolderIfRequired, optionalSyncLock), individualLogEntryErrorBehaviour) { }

		public FileLogger(Func<FileInfo> fileRetriever, bool tryToCreateFolderIfRequired, object optionalSyncLock)
			: this(fileRetriever, Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour, tryToCreateFolderIfRequired, optionalSyncLock) { }

		public FileLogger(Func<FileInfo> fileRetriever)
			: this(fileRetriever, Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour, Defaults.TryToCreateFolderIfRequired, optionalSyncLock: null) { }

		public FileLogger(FileInfo file)
			: this(file, Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour, Defaults.TryToCreateFolderIfRequired, optionalSyncLock: null) { }

		public new static class Defaults
		{
			public static Func<LogEventDetails, string> MessageFormatter { get { return TextLogger.Defaults.MessageFormatter; } }
			public static ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get { return TextLogger.Defaults.IndividualLogEntryErrorBehaviour; } }
			public static bool TryToCreateFolderIfRequired { get { return true; } }
		}

		private static Action<string> GetOutputWriter(FileInfo file, Func<FileInfo> fileRetriever, bool tryToCreateFolderIfRequired, object optionalSyncLock)
		{
			if ((file == null) && (fileRetriever == null))
				throw new ArgumentException($"One of {nameof(file)} and {nameof(fileRetriever)} must be non-null but they are both null");
			if ((file != null) && (fileRetriever != null))
				throw new ArgumentException($"Precisely one of {nameof(file)} and {nameof(fileRetriever)} must be non-null but they are both non-null");

			return content =>
			{
				FileInfo fileToWriteTo;
				if (file != null)
					fileToWriteTo = file;
				else
				{
					fileToWriteTo = fileRetriever();
					if (fileToWriteTo == null)
						throw new Exception("fileRetriever returned null FileInfo reference");
				}
				if (tryToCreateFolderIfRequired)
				{
					var folder = fileToWriteTo.Directory;
					folder.Refresh();
					if (!folder.Exists)
						folder.Create();
				}
				if (optionalSyncLock == null)
				{
					using (var writer = fileToWriteTo.AppendText())
					{
						writer.WriteLine(content);
					}
				}
				else
				{
					lock (optionalSyncLock)
					{
						using (var writer = fileToWriteTo.AppendText())
						{
							writer.WriteLine(content);
						}
					}
				}
			};        
		}
	}
}
