using System.Threading.RateLimiting;
using MySqlConnector;

namespace RateLimitingExample;

public sealed class MySqlFixedWindowRateLimiterOptions
{
    public required string ConnectionString { get; set; }
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Distributed Fixed Window rate limiter backed by MySQL.
/// Uses INSERT ON DUPLICATE KEY UPDATE for atomic counter increments.
/// </summary>
public sealed class MySqlFixedWindowRateLimiter : RateLimiter
{
    private readonly MySqlFixedWindowRateLimiterOptions _options;
    private readonly string _partitionKey;
    private long _lastActivity = Environment.TickCount64;

    public MySqlFixedWindowRateLimiter(
        string partitionKey,
        MySqlFixedWindowRateLimiterOptions options)
    {
        _partitionKey = partitionKey;
        _options = options;
    }

    public override TimeSpan? IdleDuration =>
        TimeSpan.FromMilliseconds(Environment.TickCount64 - Interlocked.Read(ref _lastActivity));

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) =>
        new MySqlRateLimitLease(false, null);

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount, CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _lastActivity, Environment.TickCount64);

        var windowSeconds = (int)_options.Window.TotalSeconds;
        // Unique window ID based on time, e.g. "w_1720000080" changes every Window seconds
        var windowId = $"w_{DateTimeOffset.UtcNow.ToUnixTimeSeconds() / windowSeconds * windowSeconds}";
        var expiresAt = DateTimeOffset.UtcNow
            .AddSeconds(windowSeconds - DateTimeOffset.UtcNow.ToUnixTimeSeconds() % windowSeconds);

        await using var conn = new MySqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        // Atomic increment: insert or update the counter, reset if expired
        await using var upsertCmd = new MySqlCommand(
            """
            INSERT INTO rate_limit_counters (partition_key, window_id, request_count, expires_at)
            VALUES (@key, @windowId, 1, @expiresAt)
            ON DUPLICATE KEY UPDATE
                request_count = IF(expires_at <= NOW(3), 1, request_count + 1),
                expires_at    = IF(expires_at <= NOW(3), @expiresAt, expires_at)
            """, conn);
        upsertCmd.Parameters.AddWithValue("@key", _partitionKey);
        upsertCmd.Parameters.AddWithValue("@windowId", windowId);
        upsertCmd.Parameters.AddWithValue("@expiresAt", expiresAt.UtcDateTime);
        await upsertCmd.ExecuteNonQueryAsync(cancellationToken);

        // Read back the current count
        await using var readCmd = new MySqlCommand(
            """
            SELECT request_count, expires_at FROM rate_limit_counters
            WHERE partition_key = @key AND window_id = @windowId
            """, conn);
        readCmd.Parameters.AddWithValue("@key", _partitionKey);
        readCmd.Parameters.AddWithValue("@windowId", windowId);

        await using var reader = await readCmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new MySqlRateLimitLease(true, null);

        var currentCount = reader.GetInt32(0);
        var windowExpiresAt = reader.GetDateTime(1);
        var retryAfter = windowExpiresAt - DateTime.UtcNow;

        if (currentCount > _options.PermitLimit)
        {
            return new MySqlRateLimitLease(false,
                retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1));
        }

        return new MySqlRateLimitLease(true, null);
    }

    private sealed class MySqlRateLimitLease : RateLimitLease
    {
        private readonly TimeSpan? _retryAfter;

        public MySqlRateLimitLease(bool isAcquired, TimeSpan? retryAfter)
        {
            IsAcquired = isAcquired;
            _retryAfter = retryAfter;
        }

        public override bool IsAcquired { get; }

        public override IEnumerable<string> MetadataNames =>
            _retryAfter.HasValue ? [MetadataName.RetryAfter.Name] : [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter.Name && _retryAfter.HasValue)
            {
                metadata = _retryAfter.Value;
                return true;
            }
            metadata = null;
            return false;
        }
    }
}
