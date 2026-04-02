using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimitingExample;

public static class RateLimiterExtensions
{
    /// <summary>
    /// Adds a distributed Fixed Window rate limiter backed by MySQL.
    /// Each partition (user/IP) gets its own counter in the database,
    /// shared across all application instances.
    /// </summary>
    public static RateLimiterOptions AddMySqlFixedWindowLimiter(
        this RateLimiterOptions options,
        string policyName,
        Action<MySqlFixedWindowRateLimiterOptions> configure)
    {
        var limiterOptions = new MySqlFixedWindowRateLimiterOptions
        {
            ConnectionString = string.Empty
        };
        configure(limiterOptions);

        options.AddPolicy(policyName, ctx =>
        {
            var partitionKey = ctx.User.Identity?.Name
                               ?? ctx.Connection.RemoteIpAddress?.ToString()
                               ?? "anonymous";

            return RateLimitPartition.Get(partitionKey, key =>
                new MySqlFixedWindowRateLimiter(key, limiterOptions));
        });

        return options;
    }
}
