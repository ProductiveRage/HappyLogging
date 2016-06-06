using System;
using System.Collections.Generic;

namespace HappyLogging
{
	/// <summary>
	/// This should throw an exception for a null messages set but whether exceptions are thrown due to any other issues (eg. null references within the
	/// messages set, messages whose ContentGenerator delegates throw exception or IO exceptions where file-writing is attempted) will vary depending
	/// upon the implementation
	/// </summary>
	public sealed class NullLogger : ILogEvents
	{
		private readonly static NullLogger _instance = new NullLogger();
		public static NullLogger Instance = _instance;
		private NullLogger() { }

		/// <summary>
		/// This should throw an exception for a null message set but whether exceptions are thrown due to any other issues (eg. a message whose ContentGenerator
		/// delegate throws an exception or IO exceptions where file-writing is attempted) will vary depending upon the implementation
		/// </summary>
		public void Log(LogEventDetails message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
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
		}
	}
}
