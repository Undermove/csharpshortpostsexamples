# VectorDataExample

Пример к посту: **[Vector Data in .NET – Building Blocks for AI Part 2](https://devblogs.microsoft.com/dotnet/vector-data-in-dotnet-building-blocks-for-ai-part-2/)**

Демонстрирует `Microsoft.Extensions.VectorData` — единый интерфейс для работы с векторными хранилищами.

## Что показывает пример

- Модель данных с атрибутами `[VectorStoreKey]`, `[VectorStoreData]`, `[VectorStoreVector]`
- Запись статей в InMemory vector store
- Семантический поиск по тексту запроса
- Поиск с фильтрацией по метаданным (`Category == "auth"`)

## Запуск

```bash
dotnet run
```

API ключи не нужны — используется InMemory хранилище и `SimpleKeywordEmbeddingGenerator`.

## В реальном проекте

Замени `SimpleKeywordEmbeddingGenerator` на настоящую модель эмбеддингов:

```csharp
// OpenAI
var embeddingGenerator = new OpenAIClient(apiKey)
    .GetEmbeddingClient("text-embedding-3-small")
    .AsIEmbeddingGenerator();

// Qdrant вместо InMemory
var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"));
```

Поддерживаемые провайдеры: Qdrant, Redis, Azure AI Search, Cosmos DB, MongoDB, Elasticsearch, Weaviate, SQLite, PostgreSQL (pgvector).

MySQL нативно не поддерживается — для SQL-баз лучший вариант PostgreSQL + pgvector.
