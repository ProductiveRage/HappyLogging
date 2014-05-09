using System;

namespace HappyLogging.Implementations
{
    /// <summary>
    /// This is just a convenience class that backs onto the ThrottlingLogger. It allows for log requests to be made asynchronously so that the caller never need worry
    /// about having to wait for the log request to complete before being able to carry on with its work. It specifies a very short buffer duration and small queue size
    /// so that messages are never delayed long and additional memory to hold the message queue never gets very large.
    /// </summary>
    public class AsyncLogger : ThrottlingLogger
    {
        public AsyncLogger(ILogEvents logger)
            : base(
                logger,
                TimeSpan.FromMilliseconds(500), // Minimum off-loading frequency
                5, // Maximum buffer size
                ThrottlingLogger.Defaults.MessageEvaluationBehaviour,
                ThrottlingLogger.Defaults.IndividualLogEntryErrorBehaviour
            ) { }
    }
}
