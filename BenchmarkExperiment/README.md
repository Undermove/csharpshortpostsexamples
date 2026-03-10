# BenchmarkExperiment

BenchmarkDotNet-проект для измерения производительности `SimpleKeywordEmbeddingGenerator.GenerateAsync`.

## Что измеряется

| Бенчмарк | Описание | Baseline |
|---|---|---|
| `SingleString` | Генерация эмбеддинга для **1 строки** | ✅ |
| `TenStrings` | Генерация эмбеддинга для **10 строк** | — |

Включён `[MemoryDiagnoser]` — измеряется выделение памяти (Allocated).

## Сборка

```bash
dotnet build BenchmarkExperiment
```

## Запуск

### Быстрый прогон (Dry job — для проверки):

```bash
dotnet run -c Release --project BenchmarkExperiment -- --job Dry --filter "*"
```

### Полный прогон:

```bash
dotnet run -c Release --project BenchmarkExperiment -- --filter "*"
```

### Конкретный бенчмарк:

```bash
dotnet run -c Release --project BenchmarkExperiment -- --filter "*SingleString*"
```

## Результаты

После запуска BenchmarkDotNet сохраняет результаты в папку `BenchmarkDotNet.Artifacts/`:
- `results/*.md` — таблица результатов
- `results/*.csv` — CSV для анализа
