using System;
using System.Text;

namespace HappyLogging
{
	public static class DefaultMessageFormatter
	{
		/// <summary>
		/// This will throw an exception for a null message argument or if the message's ContentGenerator delegate raises an exception
		/// </summary>
		public static string Format(LogEventDetails message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			try
			{
				return FormatMessage(message);
			}
			catch (Exception e)
			{
				return FormatMessage(
					new LogEventDetails(
						message.LogLevel,
						message.LogDate,
						message.ManagedThreadId,
						"Message log failure: " + e.Message,
						e
					)
				);
			}
		}

		private static string FormatMessage(LogEventDetails message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var detailedContent = new StringBuilder();
			detailedContent.AppendFormat("[{0}] [Thread{1}] ", message.LogDate.ToString("yyyy-MM-dd HH:mm:ss.fff"), message.ManagedThreadId);
			if (message.LogLevel != LogLevel.Info)
			{
				// Don't bother displaying the text "Info", it's redundant (Debug, Warning or Error are useful content, though)
				detailedContent.AppendFormat("[{0}] ", message.LogLevel.ToString());
			}
			var content = message.Content ?? message.ContentGenerator();
			if (string.IsNullOrWhiteSpace(content))
			{
				if (message.OptionalException == null)
					detailedContent.Append("{Empty Message}");
			}
			else
				detailedContent.Append(content);
			if (message.OptionalException != null)
			{
				if (!string.IsNullOrWhiteSpace(content))
					detailedContent.Append(" - ");
				detailedContent.Append(message.OptionalException.Message);
				detailedContent.AppendLine();
				detailedContent.Append(message.OptionalException.StackTrace);
				var baseException = message.OptionalException.GetBaseException();
				if ((baseException != null) && (baseException != message.OptionalException))
				{
					detailedContent.AppendFormat(" - Base Exception: {0}", baseException.Message);
					detailedContent.AppendLine();
					detailedContent.Append(baseException.StackTrace);
				}
			}
			return (detailedContent.ToString());
		}
	}
}
