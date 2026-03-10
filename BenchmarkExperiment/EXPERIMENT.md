# Эксперимент: работают ли .NET agent skills на самом деле?

Воспроизведи этот эксперимент сам и сравни результаты.

## Контекст

Microsoft выпустила [`dotnet/skills`](https://github.com/dotnet/skills) — репозиторий с agent skills
для Copilot CLI / Claude Code. Скилл `microbenchmarking` явно заявляет:

> *«Evaluations of LLMs writing BenchmarkDotNet benchmarks have revealed common failure patterns
> caused by outdated training data. Your training data likely contains outdated or incorrect BDN patterns.»*

Мы проверили — и нашли кое-что неожиданное.

## Результаты нашего эксперимента

| | Без скилла | `csharp-scripts` (не тот!) | `microbenchmarking` (правильный) |
|---|---|---|---|
| BDN версия | ❌ `0.14.0` | ❌ `0.14.0` | ✅ `0.15.8` |
| Allocation tracking | ❌ сломано | ✅ | ✅ |
| Build warnings | ❌ 2 | ✅ 0 | ✅ 0 |
| Структура проекта | ❌ хак | ✅ | ✅ |
| **Итого** | **3/7** | **6/7** | **7/7** |

Ключевые ветки:
- `experiment-copilot-without-skills` — Copilot без скилла
- `experiment-copilot-with-skills` — Copilot с `microbenchmarking` скиллом
- `experiment-without-skills` — человек без скилла
- `experiment-with-skills` — человек с `csharp-scripts` скиллом

## Задача для твоего запуска

Скопируй промпт из [`task-prompt.md`](task-prompt.md) и запусти в своём агенте.

### Шаг 1 — Без скилла

Открой новую сессию Copilot CLI / Claude Code, дай промпт из `task-prompt.md`, сохрани код.

### Шаг 2 — С правильным скиллом

```
/plugin marketplace add dotnet/skills
/plugin install dotnet-diag@dotnet-agent-skills
```

Перезапусти сессию, дай тот же промпт, сравни результат.

## Чеклист для оценки

После каждого запуска проверь:

| # | Критерий | Ожидаемое значение |
|---|---|---|
| 1 | BDN версия в csproj | НЕТ хардкода — `dotnet add package BenchmarkDotNet` |
| 2 | Return type benchmark-методов | `Task<GeneratedEmbeddings<Embedding<float>>>` (не `async Task void`) |
| 3 | Allocation tracking | `GenerateSingle` показывает > 0 байт |
| 4 | Структура проекта | `<ProjectReference>`, не `<Compile Include=...>` |
| 5 | Build warnings | 0 |
| 6 | Dry run | `dotnet run -c Release --project BenchmarkExperiment -- --job Dry --filter "*"` успешен |

## Тихая ошибка (главная находка)

Без скилла агент написал `async Task` без return значения. Dry run **проходит без ошибок**.
Но `[MemoryDiagnoser]` показывает `0 B` аллокаций — данные бенчмарка неверные.

```csharp
// ❌ Так — allocation tracking сломан, но код компилируется
[Benchmark(Baseline = true)]
public async Task SingleString()
{
    await _generator.GenerateAsync(SingleInput);
}

// ✅ Так — allocation tracking работает
[Benchmark(Baseline = true)]
public Task<GeneratedEmbeddings<Embedding<float>>> GenerateSingle()
    => _generator.GenerateAsync(SingleInput);
```

## Поделись результатами

Открой issue или PR с результатами своего запуска — интересно, изменится ли картина с другими моделями.

## Полный анализ

Критическая статья: [`ai-hub/experiments/dotnet-skills-evaluation/article.md`](https://github.com/dmitryafonchenko/ai-hub/blob/main/experiments/dotnet-skills-evaluation/article.md)
