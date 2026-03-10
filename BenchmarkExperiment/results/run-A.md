# Результаты — Запуск A (Baseline, без скилла)

## Условия
- Ветка: `experiment-without-skills`
- Агент: GitHub Copilot CLI (claude-sonnet-4.6)
- Скилл `dotnet` (microbenchmarking): НЕ установлен

## Использование токенов

| Метрика | Значение |
|---|---|
| Всего токенов | 36k / 160k (22%) |
| System/Tools | 20.9k (13%) |
| Messages | **14.6k (9%)** |
| Free Space | 86.1k (54%) |

## Итерации

Судя по токенам messages — несколько раундов правок.

## Ключевые решения агента

**Структура проекта:** Вместо `<ProjectReference>` агент слинковал исходник напрямую:
```xml
<Compile Include="..\VectorDataExample\SimpleKeywordEmbeddingGenerator.cs"
         Link="SimpleKeywordEmbeddingGenerator.cs" />
```
Плюс добавил лишний пакет `Microsoft.Extensions.AI.OpenAI Version="10.3.0"` (нужен из-за linked source).

**Program.cs:**
```csharp
BenchmarkSwitcher.FromAssembly(Assembly.GetEntryAssembly()!).Run(args);
```

## Чеклист качества

| # | Критерий | Результат | Примечание |
|---|---|---|---|
| 1 | `BenchmarkDotNet` без хардкода версии | ❌ | Захардкодил `Version="0.14.0"` — актуальная 0.15.8 |
| 2 | Benchmark-методы возвращают результат (не void) | ✅ | `Task<GeneratedEmbeddings<Embedding<float>>>` |
| 3 | Инициализация в `[GlobalSetup]` | ✅ | Правильно |
| 4 | `BenchmarkSwitcher.Run(args)` — args передан | ✅ | Через `GetEntryAssembly()` |
| 5 | Запуск через `dotnet run -c Release` | ✅ | В README |
| 6 | `[MemoryDiagnoser]` добавлен | ✅ | |
| 7 | `[Benchmark(Baseline = true)]` есть | ✅ | |
| 8 | Нет ручных loops внутри benchmark-метода | ✅ | |
| 9 | `--job Dry` запускается без исключений | ✅ | Код корректный |
| 10 | README с инструкцией | ✅ | |

**Итог: 9/10**

## Код `EmbeddingBenchmarks.cs`
```csharp
[MemoryDiagnoser]
public class EmbeddingBenchmarks
{
    private SimpleKeywordEmbeddingGenerator _generator = null!;

    [GlobalSetup]
    public void Setup() => _generator = new SimpleKeywordEmbeddingGenerator();

    [GlobalCleanup]
    public void Cleanup() => _generator.Dispose();

    [Benchmark(Baseline = true, Description = "1 строка")]
    public Task<GeneratedEmbeddings<Embedding<float>>> SingleString()
        => _generator.GenerateAsync(SingleInput);

    [Benchmark(Description = "10 строк")]
    public Task<GeneratedEmbeddings<Embedding<float>>> TenStrings()
        => _generator.GenerateAsync(TenInputs);
}
```
