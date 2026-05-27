using Microsoft.Extensions.Logging;

namespace LoggerMessageBenchmarks;

internal sealed class TestLogger : ILogger
{
    private readonly LogLevel _minLevel;

    public TestLogger(LogLevel minLevel) => _minLevel = minLevel;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        _ = formatter(state, exception);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
