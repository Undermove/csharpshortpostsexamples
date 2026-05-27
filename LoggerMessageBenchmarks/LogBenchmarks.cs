using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace LoggerMessageBenchmarks;

[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class LogBenchmarks
{
    private readonly ILogger _enabled = new TestLogger(LogLevel.Information);
    private readonly ILogger _disabled = new TestLogger(LogLevel.Warning);

    private const int UserId = 42;
    private const string Action = "checkout";
    private static readonly DateTime Timestamp = new(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc);

    // ========= Information enabled =========

    [BenchmarkCategory("Enabled"), Benchmark(Baseline = true, Description = "interpolated $\"...\"")]
    public void Interpolated_Enabled() =>
        _enabled.LogInformation($"User {UserId} did {Action} at {Timestamp:O}");

    [BenchmarkCategory("Enabled"), Benchmark(Description = "templated \"{X}\", args")]
    public void Templated_Enabled() =>
        _enabled.LogInformation("User {UserId} did {Action} at {Timestamp:O}", UserId, Action, Timestamp);

    [BenchmarkCategory("Enabled"), Benchmark(Description = "LoggerMessage.Define")]
    public void DefineDelegate_Enabled() =>
        LogMessages.UserActionDefine(_enabled, UserId, Action, Timestamp, null);

    [BenchmarkCategory("Enabled"), Benchmark(Description = "[LoggerMessage] sourcegen")]
    public void SourceGenerated_Enabled() =>
        _enabled.UserActionGen(UserId, Action, Timestamp);

    // ========= Information disabled (only Warning+ logs) =========

    [BenchmarkCategory("Disabled"), Benchmark(Description = "interpolated $\"...\"")]
    public void Interpolated_Disabled() =>
        _disabled.LogInformation($"User {UserId} did {Action} at {Timestamp:O}");

    [BenchmarkCategory("Disabled"), Benchmark(Description = "templated \"{X}\", args")]
    public void Templated_Disabled() =>
        _disabled.LogInformation("User {UserId} did {Action} at {Timestamp:O}", UserId, Action, Timestamp);

    [BenchmarkCategory("Disabled"), Benchmark(Description = "LoggerMessage.Define")]
    public void DefineDelegate_Disabled() =>
        LogMessages.UserActionDefine(_disabled, UserId, Action, Timestamp, null);

    [BenchmarkCategory("Disabled"), Benchmark(Description = "[LoggerMessage] sourcegen")]
    public void SourceGenerated_Disabled() =>
        _disabled.UserActionGen(UserId, Action, Timestamp);
}

internal static partial class LogMessages
{
    public static readonly Action<ILogger, int, string, DateTime, Exception?> UserActionDefine =
        LoggerMessage.Define<int, string, DateTime>(
            LogLevel.Information,
            new EventId(1001, nameof(UserActionDefine)),
            "User {UserId} did {Action} at {Timestamp:O}");

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "User {UserId} did {Action} at {Timestamp:O}")]
    public static partial void UserActionGen(this ILogger logger, int userId, string action, DateTime timestamp);
}
