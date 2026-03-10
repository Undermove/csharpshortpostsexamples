# Результаты — Запуск B (With Skill)

## Условия
- Ветка: `experiment-with-skills`
- Агент: GitHub Copilot CLI (claude-sonnet-4.6)
- Скилл: `dotnet` plugin (`dotnet-diag@dotnet-agent-skills`, включает microbenchmarking)

## Установка скилла
```
/plugin marketplace add dotnet/skills
/plugin install dotnet-diag@dotnet-agent-skills
```

## Использование токенов

| Метрика | Значение |
|---|---|
| Всего токенов | 28k / 160k (17%) |
| System/Tools | 20.9k (13%) |
| Messages | **6.8k (4%)** |
| Free Space | 93.9k (59%) |

**Messages tokens в 2.1x меньше** чем без скилла (6.8k vs 14.6k).

## Ключевые решения агента

**Структура проекта:** Использовал правильный `<ProjectReference>`:
```xml
<ProjectReference Include="..\VectorDataExample\VectorDataExample.csproj" />
```

Но добавил лишние свойства в csproj:
```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>  <!-- не нужно для BDN -->
<Optimize>true</Optimize>                     <!-- бессмысленно: дублирует Release config -->
```

**Program.cs:**
```csharp
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```
(более идиоматичный паттерн чем `GetEntryAssembly()`)

## Чеклист качества

| # | Критерий | Результат | Примечание |
|---|---|---|---|
| 1 | `BenchmarkDotNet` без хардкода версии | ❌ | Захардкодил `Version="0.14.0"` — актуальная 0.15.8 |
| 2 | Benchmark-методы возвращают результат (не void) | ✅ | `Task<GeneratedEmbeddings<Embedding<float>>>` |
| 3 | Инициализация в `[GlobalSetup]` | ✅ | Правильно |
| 4 | `BenchmarkSwitcher.Run(args)` — args передан | ✅ | Через `typeof(Program).Assembly` |
| 5 | Запуск через `dotnet run -c Release` | ✅ | В README |
| 6 | `[MemoryDiagnoser]` добавлен | ✅ | |
| 7 | `[Benchmark(Baseline = true)]` есть | ✅ | |
| 8 | Нет ручных loops внутри benchmark-метода | ✅ | |
| 9 | `--job Dry` запускается без исключений | ✅ | Код корректный |
| 10 | README с инструкцией | ✅ | |

**Итог: 9/10**

## Код `EmbeddingGeneratorBenchmarks.cs`
```csharp
[MemoryDiagnoser]
public class EmbeddingGeneratorBenchmarks
{
    private SimpleKeywordEmbeddingGenerator _generator = null!;

    [GlobalSetup]
    public void Setup() => _generator = new SimpleKeywordEmbeddingGenerator();

    [GlobalCleanup]
    public void Cleanup() => _generator.Dispose();

    [Benchmark(Baseline = true, Description = "1 строка")]
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateSingle()
        => _generator.GenerateAsync(SingleInput);

    [Benchmark(Description = "10 строк")]
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateTen()
        => _generator.GenerateAsync(TenInputs);
}
```
