using System;

namespace HappyLogging.Implementations
{
	/// <summary>
	/// Write log messages to the Console - this can be useful in quickly getting some output for testing but there are performance reasons to avoid this
	/// in production (see http://msdn.microsoft.com/en-us/library/system.console.aspx; although "I/O operations that use these streams are synchronized,
	/// which means that multiple threads can read from, or write to, the streams" this leads to "Do not use the Console class to display output in
	/// unattended applications, such as server applications")
	/// </summary>
	public class ConsoleLogger : TextLogger
	{
		public ConsoleLogger(Func<LogEventDetails, string> messageFormatter, ErrorBehaviourOptions individualLogEntryErrorBehaviour)
			: base(messageFormatter, content => Console.WriteLine(content), individualLogEntryErrorBehaviour) { }
		
		public ConsoleLogger() : this(Defaults.MessageFormatter, Defaults.IndividualLogEntryErrorBehaviour) { }
	}
}
