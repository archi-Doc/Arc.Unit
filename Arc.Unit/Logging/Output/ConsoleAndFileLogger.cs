// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// <see cref="ILogOutput"/> which writes logs to both <see cref="ConsoleLogger"/> and <see cref="FileLogger{TOption}"/>.
/// </summary>
public class ConsoleAndFileLogger : ILogOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleAndFileLogger"/> class.
    /// </summary>
    /// <param name="consoleLogger"><see cref="ConsoleLogger"/>.</param>
    /// <param name="fileLogger"><see cref="FileLogger{TOption}"/> of <see cref="FileLoggerOptions"/>.</param>
    public ConsoleAndFileLogger(ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    /// <inheritdoc/>
    public void Output(LogEvent logEvent)
    {
        this.consoleLogger.Output(logEvent);
        this.fileLogger.Output(logEvent);
    }

    private readonly ConsoleLogger consoleLogger;
    private readonly FileLogger<FileLoggerOptions> fileLogger;
}
