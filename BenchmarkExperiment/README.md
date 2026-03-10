# BenchmarkExperiment

Бенчмарки производительности метода `GenerateAsync` из `SimpleKeywordEmbeddingGenerator`
(проект `VectorDataExample`).

## Что измеряется

| Бенчмарк      | Описание                                 |
|---------------|------------------------------------------|
| `GenerateSingle` *(baseline)* | Генерация эмбеддинга для **1 строки**  |
| `GenerateTen`                 | Генерация эмбеддинга для **10 строк**  |

Включён `[MemoryDiagnoser]` — в результатах отображаются колонки `Allocated` и `Gen0/1/2`.

## Запуск

### Быстрый запуск (Dry job — минимум итераций, для проверки)

```bash
dotnet run -c Release --project BenchmarkExperiment -- --job Dry --filter "*"
```

### Полный прогон (точные результаты)

```bash
dotnet run -c Release --project BenchmarkExperiment -- --filter "*"
```

### Только один бенчмарк

```bash
dotnet run -c Release --project BenchmarkExperiment -- --filter "*GenerateSingle*"
```

### Сборка без запуска

```bash
dotnet build BenchmarkExperiment
```

## Пример вывода

```
| Method         | Mean      | Ratio | Allocated |
|--------------- |----------:|------:|----------:|
| 1 строка       | 1.23 μs   |  1.00 |     640 B |
| 10 строк       | 9.87 μs   |  8.02 |   4,320 B |
```

> **Важно:** BenchmarkDotNet требует запуска в конфигурации `Release`.  
> В режиме `Debug` бенчмарки не запускаются (выводится предупреждение).
