using System.Data;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using IAsyncEnumerableExample.Data;
using IAsyncEnumerableExample.Infrastructure;
using IAsyncEnumerableExample.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MySQL")
    ?? "Server=localhost;Port=3306;Database=knowledgebase;User=root;Password=knowledgebase;";

var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

// Фабрика контекстов, а не scoped-контекст. Для стриминга это принципиально:
// IAsyncEnumerable перечисляется уже ПОСЛЕ выхода из обработчика, и scoped-контекст
// успел бы закрыться прямо посреди стрима. Фабрика + await using держат контекст
// ровно столько, сколько идёт перечисление.
builder.Services.AddDbContextFactory<KnowledgeBaseContext>(opt =>
    opt.UseMySql(connectionString, serverVersion));

builder.Services.AddSingleton<MemorySampler>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MemorySampler>());

// Единый JSON-энкодер для всех трёх вариантов (list/sse через DI, ndjson — вручную),
// чтобы экранирование совпадало и сравнение по байтам было честным.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

// CORS для дев-фронта (Vite). Для демо пускаем всех.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

var jsonWeb = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
byte[] newline = [(byte)'\n'];

// ── Сидинг демо-данных при старте ───────────────────────────────────────────
var articleCount = int.TryParse(Environment.GetEnvironmentVariable("ARTICLE_COUNT"), out var c) ? c : 500;
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KnowledgeBaseContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await DbSeeder.SeedAsync(db, articleCount, approxSizeBytes: 234_000);
}

// Общий запрос: полные статьи (~234 КБ тело) по порядку, без трекинга. ?count — сколько штук.
static IQueryable<Article> Query(KnowledgeBaseContext db, int? count)
{
    IQueryable<Article> q = db.Articles.AsNoTracking().OrderBy(a => a.Id);
    return count is > 0 ? q.Take(count.Value) : q;
}

// ════════════════════════════════════════════════════════════════════════════
// Три варианта отдать список статей — на одних и тех же данных
// ════════════════════════════════════════════════════════════════════════════

// 1) LIST — буфер: материализуем ВСЕ статьи в память, потом сериализуем одним
// JSON-массивом. Клиент не видит ничего до конца. Пик памяти = весь датасет.
app.MapGet("/articles/list", async (IDbContextFactory<KnowledgeBaseContext> factory,
    int? count, CancellationToken ct) =>
{
    await using var db = await factory.CreateDbContextAsync(ct);
    return Results.Ok(await Query(db, count).ToListAsync(ct));
});

// 2) NDJSON — стрим: по одному JSON-объекту на строку, флашим сразу.
// EF тянет строки по одной (AsAsyncEnumerable), память ≈ одна строка.
app.MapGet("/articles/ndjson", async (HttpContext http, IDbContextFactory<KnowledgeBaseContext> factory,
    int? count, CancellationToken ct) =>
{
    http.Response.ContentType = "application/x-ndjson";
    await using var db = await factory.CreateDbContextAsync(ct);
    await foreach (var a in Query(db, count).AsAsyncEnumerable().WithCancellation(ct))
    {
        await JsonSerializer.SerializeAsync(http.Response.Body, a, jsonWeb, ct);
        await http.Response.Body.WriteAsync(newline, ct);
        await http.Response.Body.FlushAsync(ct); // гоним строку клиенту немедленно
    }
});

// 3) SSE — стрим через встроенный хелпер .NET 10: сам сериализует каждый объект
// в data: и сам делает SSE-обёртку. Никаких ручных Write/Flush/циклов парсинга.
app.MapGet("/articles/sse", (IDbContextFactory<KnowledgeBaseContext> factory,
    int? count, CancellationToken ct)
    => TypedResults.ServerSentEvents(StreamSse(factory, count, ct)));

static async IAsyncEnumerable<SseItem<Article?>> StreamSse(
    IDbContextFactory<KnowledgeBaseContext> factory, int? count,
    [EnumeratorCancellation] CancellationToken ct)
{
    await using var db = await factory.CreateDbContextAsync(ct);
    await foreach (var a in Query(db, count).AsAsyncEnumerable().WithCancellation(ct))
        yield return new SseItem<Article?>(a, "article");
    // Финальное событие: фронт по нему закроет EventSource (иначе тот авто-реконнектится).
    yield return new SseItem<Article?>(null, "done");
}

// ════════════════════════════════════════════════════════════════════════════
// ОДИН большой объект: GetString vs GetTextReader vs GetStream
// ════════════════════════════════════════════════════════════════════════════

// ❌ EF + GetString: весь LONGTEXT материализуется в одну string → Large Object Heap.
app.MapGet("/article/{id:int}/getstring", async (int id, IDbContextFactory<KnowledgeBaseContext> factory) =>
{
    await using var db = await factory.CreateDbContextAsync();
    var a = await db.Articles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    return a is null ? Results.NotFound() : Results.Text(a.ContentJson, "application/json");
});

// ⚠️ GetTextReader на LONGTEXT: ВЫГЛЯДИТ как стрим, но у MySqlConnector внутри это
// new StringReader(GetString(...)) — весь объект уже в LOH. Экономии ноль.
app.MapGet("/article/{id:int}/gettextreader", async (int id, IDbContextFactory<KnowledgeBaseContext> factory, CancellationToken ct) =>
{
    var db = await factory.CreateDbContextAsync(ct);
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync(ct);
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT ContentJson FROM articles WHERE Id = @id";
    var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; cmd.Parameters.Add(p);

    var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow, ct);
    if (!await reader.ReadAsync(ct)) { await reader.DisposeAsync(); await db.DisposeAsync(); return Results.NotFound(); }

    return Results.Stream(async body =>
    {
        await using (db)
        await using (reader)
        {
            using var tr = reader.GetTextReader(0);   // ← внутри уже GetString → весь объект в LOH
            await using var w = new StreamWriter(body, new UTF8Encoding(false), leaveOpen: true);
            var buf = new char[8192];
            int n;
            while ((n = await tr.ReadAsync(buf, ct)) > 0) await w.WriteAsync(buf.AsMemory(0, n), ct);
            await w.FlushAsync(ct);
        }
    }, "application/json");
});

// ✅ GetStream на LONGBLOB: реальный стрим мелкими буферами мимо LOH, прямо в ответ.
app.MapGet("/article/{id:int}/getstream", async (int id, IDbContextFactory<KnowledgeBaseContext> factory, CancellationToken ct) =>
{
    var db = await factory.CreateDbContextAsync(ct);
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync(ct);
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT ContentBlob FROM articles WHERE Id = @id";
    var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; cmd.Parameters.Add(p);

    var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow, ct);
    if (!await reader.ReadAsync(ct)) { await reader.DisposeAsync(); await db.DisposeAsync(); return Results.NotFound(); }

    return Results.Stream(async body =>
    {
        await using (db)
        await using (reader)
        await using (var src = reader.GetStream(0))   // настоящий поток, объект целиком в память не лезет
            await src.CopyToAsync(body, ct);
    }, "application/json");
});

// ── Наблюдаем за памятью процесса ────────────────────────────────────────────
app.MapGet("/memory", (MemorySampler sampler) => Results.Ok(new
{
    peakManagedHeapMb = Math.Round(sampler.PeakBytes / 1024d / 1024d, 1),
    currentManagedHeapMb = Math.Round(GC.GetTotalMemory(false) / 1024d / 1024d, 1),
    workingSetMb = Math.Round(Environment.WorkingSet / 1024d / 1024d, 1),
    totalAllocatedMb = Math.Round(GC.GetTotalAllocatedBytes() / 1024d / 1024d, 0),
    gen0 = GC.CollectionCount(0),
    gen1 = GC.CollectionCount(1),
    gen2 = GC.CollectionCount(2)
}));

// Сбросить пик перед замером конкретной ручки.
// Компактим LOH: большие строки (234 КБ) живут в Large Object Heap, который обычная
// сборка не возвращает ОС — без компакта пик «протёк» бы между вариантами.
app.MapPost("/memory/reset", (MemorySampler sampler) =>
{
    System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
        System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    sampler.Reset();
    return Results.Ok(new { reset = true });
});

app.Run();
