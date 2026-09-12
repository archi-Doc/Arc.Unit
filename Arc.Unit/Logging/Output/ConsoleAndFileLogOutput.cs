// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// <see cref="ILogOutput"/> which writes logs to both <see cref="ConsoleLogOutput"/> and <see cref="FileLogOutput{TOptions}"/>.
/// </summary>
public class ConsoleAndFileLogOutput : ILogOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleAndFileLogOutput"/> class.
    /// </summary>
    /// <param name="consoleLogOutput"><see cref="ConsoleLogOutput"/>.</param>
    /// <param name="fileLogOutput"><see cref="FileLogOutput{TOptions}"/> of <see cref="FileLogOutputOptions"/>.</param>
    public ConsoleAndFileLogOutput(ConsoleLogOutput consoleLogOutput, FileLogOutput<FileLogOutputOptions> fileLogOutput)
    {
        this.consoleLogOutput = consoleLogOutput;
        this.fileLogOutput = fileLogOutput;
    }

    /// <inheritdoc/>
    public void Output(LogEvent logEvent)
    {
        this.consoleLogOutput.Output(logEvent);
        this.fileLogOutput.Output(logEvent);
    }

    private readonly ConsoleLogOutput consoleLogOutput;
    private readonly FileLogOutput<FileLogOutputOptions> fileLogOutput;
}
