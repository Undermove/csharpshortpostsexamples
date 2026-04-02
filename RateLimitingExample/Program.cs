using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    // Глобальный лимитер — отдельное ведро токенов на каждого юзера
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: ctx.User.Identity?.Name
                          ?? ctx.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 5
            }));

    // Жёсткий лимит для эндпоинтов с LLM
    options.AddTokenBucketLimiter("llm-ask", opt =>
    {
        opt.TokenLimit = 10;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.TokensPerPeriod = 2;
    });

    // Мягкий лимит для обычных эндпоинтов
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Красивый ответ при превышении лимита
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        await context.HttpContext.Response.WriteAsync(
            "Слишком много запросов, попробуй позже", token);
    };
});

var app = builder.Build();

app.UseRateLimiter();

// Эндпоинт с LLM — жёсткий лимит
app.MapPost("/ask", (QuestionRequest request) =>
{
    // Тут был бы вызов LLM, но для примера просто эхо
    return Results.Ok(new { answer = $"Ответ на вопрос: {request.Question}" });
}).RequireRateLimiting("llm-ask");

// Обычный эндпоинт — мягкий лимит
app.MapGet("/articles", () =>
{
    return Results.Ok(new[]
    {
        new { id = 1, title = "Rate Limiting в ASP.NET Core" },
        new { id = 2, title = "Semantic Kernel для мини-агентов" },
        new { id = 3, title = "Vector Data — EF для векторов" }
    });
}).RequireRateLimiting("general");

// Health-чек — без лимитов
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

record QuestionRequest(string Question);
