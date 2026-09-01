// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

public interface IFileLogger
{
    string GetCurrentPath();

    void DeleteAllLogs();

    Task<int> Flush(bool terminate);
}

public class FileLogger<TOption> : BufferedLogOutput, IFileLogger
    where TOption : FileLoggerOptions
{
    public FileLogger(ExecutionRoot root, LogUnit logUnit, TOption options)
        : base(logUnit)
    {
        if (string.IsNullOrEmpty(Path.GetDirectoryName(options.Path)))
        {
            options = options with { Path = Path.Combine(Directory.GetCurrentDirectory(), options.Path), };
        }

        this.worker = new(root, options);
        this.options = options;
        this.worker.SendSignal(ExecutionSignal.Start);
    }

    public string GetCurrentPath()
        => this.worker.GetCurrentPath();

    public void DeleteAllLogs()
        => this.worker.LimitLogs(true);

    public override void Output(LogEvent param)
    {
        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(param);
        }
    }

    public override Task<int> Flush(bool terminate) => this.worker.Flush(terminate);

    private readonly FileLoggerWorker worker;
    private readonly TOption options;
}
