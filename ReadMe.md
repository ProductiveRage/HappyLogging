# Happy Logging

This is intended to be a consolidation of the most common logging patterns that I tend to use and have duplicated in several places. Now they're in one simple library!

Sometimes I just want to push messages to the console. Sometimes I want to push information to a trace (if anyone's listening) for a long-running service but log information about errors to disk. I want to defer generation of log messages so that if there are any expensive-to-generate debug messages then the work to generate those messages is only performed if someone is recording the messages. Sometimes I want to ignore a component's messages and send them all to a **NullLogger**. The aim of this library is to make all of these scenarios and others as simple as possible.

Any class or method that requires this logging should take an **ILogEvents** reference (that can be provided by your favourite Dependency Injection framework, if you're into that sort of thing). There are then extension methods to try to make logging as simple as possible - eg.

    logger.LogIgnoringAnyError(LogLevel.Warning, () => "I don't know what's happening!");

**LogLevel** is an enum with the values Debug, Info, Warning and Error. The delegate is to delay the generation of messages. The "LogIgnoringAnyError" methods wrap the work in a try..catch in case anything goes wrong during the logging - my logic being that if logging has failed, what are you going to? Log the failure??

All log messages require a DateTime and ManagedThreadId. The above signature uses DateTime.Now and Thread.CurrentThread.ManagedThreadId but there are signatures where they can be specified explicitly if required. An exception may also be provided - probably only applicable for Error messages but I'm not picky about requiring that they only be included with Errors.

## Getting fancy

Having read the above, hopefully it's fairly obvious what the **ConsoleLogger**, **TraceLogger**, **FileLogger** and **NullLogger** do. Likewise there's a **FilteredLogger** (wraps another logger but only passes through messages with particular log levels) and a **CombinedLogger** (wraps multiple loggers and passes through messages to each of them). The **ConsoleLogger**, **TraceLogger** and **FileLogger** all generate messages in a common format but they all allow for custom formatters to be provided if desired.

Then there's the **ThrottlingLogger** which will batch up messages and dispatch them at intervals (or when the number of batched-up messages reaches a certain threshold). This works well with the **FileLogger** if a single logger instance can be shared between all requests that a service is responsible for hosting, since it prevents file-locking issues with writes from multiple threads. (The **ThrottlingLogger** happily accepts multi-threaded requests into its "batching up" queue but guarantees that only a single thread at a time will actually call into the logger that it wraps).

Finally there's the **ErrorWithBackTrackLogger**. This will also maintain a queue of messages but it will throw most of them away until an error message arrives. When this happens, it will pass through the most recent messages of *all* log levels through to the wrapped logger, along with the error. This means that when an error is logged, detailed information about what was happening leading up to it is available without detailed logs having to be written at all times. Debug messages could be logged with all sorts of expensive serialised object graphs but the actual work to generate these would only be performed when an error actually blows up, when the information is most useful. Hurrah!

## Configuring loggers

Hopefully the classes are fairly straight forward, and they're all commented to explain any funny little nuances. However, there's a **DefaultLoggerFactory** class which demonstrates some interesting (and hopefully useful) variations.

Or how about this:

    var logger = new CombinedLogger(
      new FilteredLogger(
        new TraceLogger(),
        LogLevel.Info, LogLevel.Warning, LogLevel.Error
      ),
      new ThrottlingLogger(
        new ErrorWithBackTrackLogger(
          new FileLogger(
            new FileInfo("Errors.log")
          )
        )
      )
    );

Write Info, Warning and Error messages to the trace but if an Error *does* occur then also write it and what was happening (Debug, Info, Warning messages) leading up to it away to disk for further analysis!

The default message formatter will include a stack trace if an exception is specified, along with the stack trace for the base exception for cases where the root cause has been wrapped into another exception by a presumably well-meaning class that nonetheless may have obscured where the problem really originated.

The **FileLogger** has constructor signatures that take a delegate that returns a **FileInfo** reference so it's easy to generate filenames based upon the date and time. This is demonstrated by the **DefaultLoggerFactory**, as is a way to prevent log files from getting too large.