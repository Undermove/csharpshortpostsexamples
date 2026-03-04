// Microsoft.Extensions.VectorData — семантический поиск по базе знаний
// Пример к посту: https://devblogs.microsoft.com/dotnet/vector-data-in-dotnet-building-blocks-for-ai-part-2/
//
// Запуск: dotnet run
// API ключи не нужны — используется InMemory хранилище и простой локальный эмбеддинг

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using VectorDataExample;

// --- Настройка ---
// Для реального проекта замените SimpleKeywordEmbeddingGenerator на:
// new OpenAIClient(apiKey).GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator()
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new SimpleKeywordEmbeddingGenerator();

var vectorStore = new InMemoryVectorStore();
var collection = vectorStore.GetCollection<string, KnowledgeArticle>("knowledge-base");
await collection.EnsureCollectionExistsAsync();

// --- Наполняем базу знаний ---
var articles = new[]
{
    new KnowledgeArticle { Id = "1", Title = "Сброс пароля",              Text = "Как восстановить и сбросить пароль от аккаунта пользователя через email", Category = "auth" },
    new KnowledgeArticle { Id = "2", Title = "Двухфакторная аутентификация", Text = "Настройка двухфакторной защиты аутентификации 2FA с TOTP кодами Google Authenticator", Category = "auth" },
    new KnowledgeArticle { Id = "3", Title = "Оплата заказа",              Text = "Способы оплаты заказа картой банковской или через СБП", Category = "payment" },
    new KnowledgeArticle { Id = "4", Title = "Возврат средств",            Text = "Как оформить возврат денег и средств за отменённый заказ", Category = "payment" },
    new KnowledgeArticle { Id = "5", Title = "Смена email адреса",         Text = "Изменение email адреса почты и логина в личном кабинете аккаунта", Category = "auth" },
};

// Генерируем эмбеддинги для всех статей разом
var texts = articles.Select(a => a.Text).ToList();
var embeddings = await embeddingGenerator.GenerateAsync(texts);
for (int i = 0; i < articles.Length; i++)
    articles[i].Embedding = embeddings[i].Vector;

await collection.UpsertAsync(articles);
Console.WriteLine($"✅ Загружено {articles.Length} статей в базу знаний\n");

// --- Семантический поиск ---
var queries = new[]
{
    "забыл пароль от аккаунта",
    "хочу включить двухфакторную защиту",
    "оплата заказа картой",
};

foreach (var query in queries)
{
    Console.WriteLine($"🔍 Запрос: \"{query}\"");

    var queryEmbedding = await embeddingGenerator.GenerateAsync([query]);

    var results = collection.SearchAsync(queryEmbedding[0].Vector, top: 2);
    await foreach (var result in results)
    {
        Console.WriteLine($"   [{result.Score:F2}] {result.Record.Title} — {result.Record.Text}");
    }
    Console.WriteLine();
}

// --- Поиск с фильтром по категории ---
Console.WriteLine("🎯 Поиск только по категории 'auth': \"проблема со входом в аккаунт\"");
var filterQuery = await embeddingGenerator.GenerateAsync(["проблема со входом в аккаунт"]);
var filteredResults = collection.SearchAsync(
    filterQuery[0].Vector,
    top: 3,
    new VectorSearchOptions<KnowledgeArticle> { Filter = r => r.Category == "auth" });

await foreach (var result in filteredResults)
{
    Console.WriteLine($"   [{result.Score:F2}] {result.Record.Title}");
}
