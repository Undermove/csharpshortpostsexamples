using HttpQueryExample;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Имитация корпоративного прокси/WAF, который режет незнакомые методы.
// Фронт включает её заголовком X-Simulate-Waf — так можно пощупать fallback на POST.
app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsQuery(ctx.Request.Method) && ctx.Request.Headers.ContainsKey("X-Simulate-Waf"))
    {
        ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        await ctx.Response.WriteAsJsonAsync(new { error = "QUERY зарезан «корпоративным прокси» (симуляция)" });
        return;
    }

    await next();
});

// Один хендлер на оба метода: QUERY — для тех, кто умеет, POST — fallback для тех, кто за прокси
app.MapQuery("/products/search", SearchAsync);
app.MapPost("/products/search", SearchAsync);

app.Run();

// параметр HttpRequest, а не HttpContext: хендлер вида HttpContext → Task<IResult>
// ASP.NET сочтёт за RequestDelegate и молча выбросит IResult (ASP0016)
static async Task<IResult> SearchAsync(HttpRequest request)
{
    var filter = await request.ReadFromJsonAsync<ProductFilter>();
    if (filter is null)
        return Results.BadRequest(new { error = "Тело запроса должно быть JSON с фильтром" });

    IEnumerable<Product> result = Catalog.Products;

    if (!string.IsNullOrWhiteSpace(filter.Text))
        result = result.Where(p => p.Name.Contains(filter.Text, StringComparison.OrdinalIgnoreCase));

    if (filter.Categories is { Length: > 0 })
        result = result.Where(p => filter.Categories.Contains(p.Category));

    if (filter.MaxPrice is { } max)
        result = result.Where(p => p.Price <= max);

    // небольшая задержка, чтобы в UI было видно, что запрос «летит»
    await Task.Delay(150);

    return Results.Ok(new { method = request.Method, items = result.ToArray() });
}

public record ProductFilter(string? Text, string[]? Categories, decimal? MaxPrice);

public record Product(string Name, string Category, decimal Price);

public static class Catalog
{
    public static readonly Product[] Products =
    [
        new("Keychron Q1 Pro", "клавиатуры", 199),
        new("HHKB Professional Hybrid", "клавиатуры", 385),
        new("NuPhy Air75 V2", "клавиатуры", 140),
        new("Logitech MX Master 3S", "мыши", 99),
        new("Razer Viper V3 Pro", "мыши", 160),
        new("Glorious Model O", "мыши", 60),
        new("LG UltraFine 27\" 5K", "мониторы", 1299),
        new("Dell U2723QE 4K", "мониторы", 620),
        new("BenQ RD280U (для кода)", "мониторы", 600),
        new("Herman Miller Aeron", "кресла", 1450),
        new("Anda Seat Kaiser 3", "кресла", 500),
        new("Секретлаб Titan Evo", "кресла", 550),
        new("Sony WH-1000XM5", "наушники", 350),
        new("Apple AirPods Max", "наушники", 480),
        new("Marshall Major V", "наушники", 130),
    ];
}
