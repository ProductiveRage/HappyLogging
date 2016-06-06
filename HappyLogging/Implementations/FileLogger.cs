using System;
using System.IO;

namespace HappyLogging.Implementations
{
	/// <summary>
	/// Append log messages to a file
	/// </summary>
	public sealed class FileLogger : TextLogger
	{
		public FileLogger(Func<FileInfo> fileRetriever, Func<LogEventDetails, string> messageFormatter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
			: base(messageFormatter, GetOutputWriter(fileRetriever), individualLogEntryErrorBehaviour) { }

		public FileLogger(FileInfo file, Func<LogEventDetails, string> messageFormatter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
			: this(() => file, messageFormatter, individualLogEntryErrorBehaviour)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
		}
		
		public FileLogger(Func<FileInfo> fileRetriever) : this(fileRetriever, Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour) { }
		
		public FileLogger(FileInfo file) : this(file, Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour) { }

		private static Action<string> GetOutputWriter(Func<FileInfo> fileRetriever)
		{
			if (fileRetriever == null)
				throw new ArgumentNullException(nameof(fileRetriever));

			return content =>
			{
				var file = fileRetriever();
				if (file == null)
					throw new Exception("fileRetriever returned null FileInfo reference");
				using (var writer = file.AppendText())
				{
					writer.WriteLine(content);
				}
			};        
		}
	}
}
