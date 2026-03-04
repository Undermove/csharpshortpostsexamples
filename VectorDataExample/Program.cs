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

Console.WriteLine("💡 Примеры запросов:");
Console.WriteLine("   • забыл пароль от аккаунта");
Console.WriteLine("   • хочу включить двухфакторную защиту");
Console.WriteLine("   • оплата заказа картой");
Console.WriteLine("   • как поменять email");
Console.WriteLine("   • вернуть деньги за отменённый заказ");
Console.WriteLine("\n💬 Введите запрос для поиска по базе знаний (или 'выход' для завершения):");

while (true)
{
    Console.Write("\n> ");
    var userInput = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(userInput))
        continue;

    if (userInput.Equals("выход", StringComparison.OrdinalIgnoreCase) ||
        userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    var userEmbedding = await embeddingGenerator.GenerateAsync([userInput]);
    var userResults = collection.SearchAsync(userEmbedding[0].Vector, top: 3);

    Console.WriteLine($"🔍 Результаты по запросу: \"{userInput}\"");
    var found = false;
    await foreach (var result in userResults)
    {
        found = true;
        Console.WriteLine($"   [{result.Score:F2}] {result.Record.Title} — {result.Record.Text}");
    }
    if (!found)
        Console.WriteLine("   Ничего не найдено.");
}
