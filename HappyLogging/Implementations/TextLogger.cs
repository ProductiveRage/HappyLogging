using System;
using System.Collections.Generic;
using System.Text;

namespace HappyLogging
{
	/// <summary>
	/// Write log messages to the Console
	/// </summary>
	public abstract class TextLogger : ILogEvents
	{
		private readonly Func<LogEventDetails, string> _messageFormatter;
		private readonly Action<string> _outputWriter;
		protected TextLogger(Func<LogEventDetails, string> messageFormatter, Action<string> outputWriter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
		{
			if (messageFormatter == null)
				throw new ArgumentNullException(nameof(messageFormatter));
			if (outputWriter == null)
				throw new ArgumentNullException(nameof(outputWriter));
			if ((individualLogEntryErrorBehaviour != ErrorBehaviourOptions.Ignore) && (individualLogEntryErrorBehaviour != ErrorBehaviourOptions.ThrowException))
			{
				// Note: Explicitly check for all valid values rather than using Enum.IsDefined since IsDefined uses reflection and logging should be as cheap as possible
				// (so reflection is best avoided)
				throw new ArgumentOutOfRangeException(nameof(individualLogEntryErrorBehaviour));
			}

			IndividualLogEntryErrorBehaviour = individualLogEntryErrorBehaviour;
			_messageFormatter = messageFormatter;
			_outputWriter = outputWriter;
		}

		public static class Defaults
		{
			public static Func<LogEventDetails, string> MessageFormatter { get { return DefaultMessageFormatter.Format; } }
			public static ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get { return ErrorBehaviourOptions.Ignore; } }
		}

		public ErrorBehaviourOptions IndividualLogEntryErrorBehaviour { get; }

		/// <summary>
		/// This should throw an exception for a null message set but whether exceptions are thrown due to any other issues (eg. a message whose ContentGenerator
		/// delegate throws an exception or IO exceptions where file-writing is attempted) will vary depending upon the implementation
		/// </summary>
		public void Log(LogEventDetails message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			string messageContentToDisplay;
			try
			{
				messageContentToDisplay = _messageFormatter(message);
				if (messageContentToDisplay == null)
					throw new Exception("messageFormatter returned null");
			}
			catch
			{
				if (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException)
					throw;
				return;
			}
			if (messageContentToDisplay == "")
				return;
			_outputWriter(messageContentToDisplay.ToString());
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

			var combinedContentBuilder = new StringBuilder();
			var isFirstMessage = true;
			foreach (var message in messages)
			{
				if ((message == null) && (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException))
					throw new ArgumentException("Null reference encountered in messages set");

				string messageContentToDisplay;
				try
				{
					messageContentToDisplay = _messageFormatter(message);
					if (messageContentToDisplay == null)
						throw new Exception("messageFormatter returned null");
				}
				catch
				{
					if (IndividualLogEntryErrorBehaviour == ErrorBehaviourOptions.ThrowException)
						throw;
					continue;
				}
				if (!isFirstMessage)
					combinedContentBuilder.AppendLine();
				combinedContentBuilder.Append(messageContentToDisplay);
				isFirstMessage = false;
			}
			if (combinedContentBuilder.Length > 0)
				_outputWriter(combinedContentBuilder.ToString());
		}
	}
}
