using System;
using System.Collections.Generic;

namespace HappyLogging.Implementations
{
	/// <summary>
	/// This should throw an exception for a null messages set but whether exceptions are thrown due to any other issues (eg. null references within the
	/// messages set, messages whose ContentGenerator delegates throw exception or IO exceptions where file-writing is attempted) will vary depending
	/// upon the implementation
	/// </summary>
	public class NullLogger : ILogEvents
	{
		public void Log(IEnumerable<LogEventDetails> messages)
		{
			if (messages == null)
				throw new ArgumentNullException("messages");
		}
	}
}
