# Сводный анализ эксперимента

## Запуски

Три набора данных по одной задаче (BenchmarkDotNet для `SimpleKeywordEmbeddingGenerator`):

| | Запуск 1: человек без скилла | Запуск 2: человек с `csharp-scripts` скиллом | Запуск 3: Copilot без скилла | Запуск 4: Copilot с `microbenchmarking` скиллом |
|---|---|---|---|---|
| Ветка | `experiment-without-skills` | `experiment-with-skills` | `experiment-copilot-without-skills` | `experiment-copilot-with-skills` |
| BDN версия | `0.14.0` ❌ | `0.14.0` ❌ | `0.14.0` ❌ | **`0.15.8`** ✅ |
| Benchmark return type | `Task<...>` ✅ | `Task<...>` ✅ | **`async Task` void** ❌ | `Task<...>` ✅ |
| Allocation tracking | ✅ | ✅ | **Broken (0 B)** ❌ | ✅ |
| Структура проекта | `<Compile>` хак ❌ | `ProjectReference` ✅ | `<Compile>` хак ❌ | `ProjectReference` ✅ |
| Build warnings | 2 ❌ | 0 ✅ | 2 ❌ | **0** ✅ |
| Message tokens | 14.6k | 6.8k | — | — |

## Ключевые находки

### 1. Правильный скилл имеет значение

Человек запускал `csharp-scripts` скилл (для one-file .NET 10 скриптов — не для BDN проектов).
Тем не менее часть результатов улучшилась (структура проекта). Скилл сработал как generic boost,
а не через BDN-специфичное знание.

Copilot с **правильным** скиллом (`microbenchmarking`) исправил конкретные BDN-ошибки:
- `dotnet add package BenchmarkDotNet` → `0.15.8` вместо хардкода `0.14.0`
- Return type `Task<GeneratedEmbeddings<float>>` → allocation tracking работает

### 2. Void async Task — тихая поломка измерений

Без скилла Copilot написал `async Task` без return значения. `--job Dry` **прошёл без ошибок**,
но `[MemoryDiagnoser]` показал `0 B` для SingleString. Это тихая ошибка: код компилируется,
запускается, но данные неверные. Скилл явно предупреждает: «Return results from benchmark methods
to prevent dead code elimination».

### 3. Версия BDN — единственное что исправил только правильный скилл

Все три запуска без `microbenchmarking` скилла написали `0.14.0`. Только Copilot с правильным
скиллом использовал `dotnet add package BenchmarkDotNet` и получил `0.15.8`.

## Итоговый чеклист

| Критерий из SKILL.md | Без скилла | `csharp-scripts` | `microbenchmarking` |
|---|---|---|---|
| BDN без хардкода версии | ❌ | ❌ | ✅ |
| Return value (не void) | ❌ | ✅ | ✅ |
| `[GlobalSetup]` | ✅ | ✅ | ✅ |
| `args` передан в `BenchmarkSwitcher` | ✅ | ✅ | ✅ |
| `ProjectReference` (не `<Compile>` хак) | ❌ | ✅ | ✅ |
| 0 build warnings | ❌ | ✅ | ✅ |
| Allocation tracking работает | ❌ | ✅ | ✅ |
| **Итого** | **3/7** | **6/7** | **7/7** |
