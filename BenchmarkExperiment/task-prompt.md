# Задача для агента

Скопируй этот промпт ТОЧНО и полностью в каждую сессию. Не добавляй ничего, не убирай.

---

В этом .NET solution есть проект `VectorDataExample` с классом `SimpleKeywordEmbeddingGenerator`
(файл `VectorDataExample/SimpleKeywordEmbeddingGenerator.cs`), который реализует
`IEmbeddingGenerator<string, Embedding<float>>`.

Создай новый BenchmarkDotNet-проект `BenchmarkExperiment` в папке `BenchmarkExperiment/`
рядом с остальными проектами.

Бенчмарк должен измерять производительность метода `GenerateAsync` из `SimpleKeywordEmbeddingGenerator`:
- Случай 1: генерация эмбеддинга для 1 строки
- Случай 2: генерация эмбеддинга для 10 строк
- Включи замер выделения памяти
- Добавь baseline

Требования:
- Проект должен собираться командой `dotnet build BenchmarkExperiment`
- Запускаться командой: `dotnet run -c Release --project BenchmarkExperiment -- --job Dry --filter "*"`
- Добавь README с инструкцией как запустить

---
