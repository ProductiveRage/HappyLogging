using System;

namespace HappyLogging
{
    public class LogEventDetails
    {
        public LogEventDetails(LogLevel logLevel, DateTime logDate, int managedThreadId, Func<string> contentGenerator, Exception optionalException)
        {
            if (!Enum.IsDefined(typeof(LogLevel), logLevel))
                throw new ArgumentOutOfRangeException("logLevel");
            if (contentGenerator == null)
                throw new ArgumentNullException("contentGenerator");
            var content = contentGenerator();
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Null/empty content specified");

            LogLevel = logLevel;
            LogDate = logDate;
            ManagedThreadId = managedThreadId;
            ContentGenerator = contentGenerator;
            OptionalException = optionalException;
        }

        public LogLevel LogLevel { get; private set; }

        public DateTime LogDate { get; private set; }

        public int ManagedThreadId { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public Func<string> ContentGenerator { get; private set; }

        /// <summary>
        /// This may be null as it is optional information (there is no guarantee that it will be non-null even if the LogLevel is Error)
        /// </summary>
        public Exception OptionalException { get; private set; }
    }
}
