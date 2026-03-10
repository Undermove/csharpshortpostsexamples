# Эксперимент: dotnet/skills — реально ли скилл помогает?

## Гипотеза

Скилл `microbenchmarking` из [`dotnet/skills`](https://github.com/dotnet/skills) должен улучшить качество
написанных агентом BenchmarkDotNet-бенчмарков. Сам скилл признаёт:

> *"Evaluations of LLMs writing BenchmarkDotNet benchmarks have revealed common failure patterns
> caused by outdated assumptions about BDN's behavior — particularly around runtime comparison,
> job configuration, and execution defaults that have changed in recent versions."*

Это редкий случай, когда авторы скилла прямо говорят: «модель ошибается в этой теме».
Проверяем, действительно ли скилл это исправляет.

## Задача для агента (одинаковая в обоих запусках)

Задачу скопируй ТОЧНО из файла `task-prompt.md`.

## Условия

### Запуск A — Baseline (без скилла)

1. Открой **новую** сессию GitHub Copilot CLI (`gh copilot`)
2. Убедись, что скилл `microbenchmarking` **не установлен** (`/skills` — его не должно быть в списке)
3. Дай агенту задачу из `task-prompt.md`
4. Реши задачу в папке `BenchmarkExperiment/`
5. После того как агент написал код — **не исправляй** вручную, сохрани как есть
6. Запись результата: сохрани финальный код коммитом в ветку `experiment/baseline`

### Запуск B — With Skill

1. Открой **новую** сессию GitHub Copilot CLI
2. Установи скилл:
   ```
   /plugin marketplace add dotnet/skills
   /plugin install dotnet-diag@dotnet-agent-skills
   ```
3. Убедись что скилл появился (`/skills`)
4. Дай агенту **ту же самую задачу** из `task-prompt.md`
5. Реши задачу в папке `BenchmarkExperiment/` (сначала откати изменения от Запуска A: `git checkout -- BenchmarkExperiment/`)
6. Запись результата: сохрани коммитом в ветку `experiment/with-skill`

## Что записывать

По каждому запуску зафиксируй в файле `results/run-A.md` / `results/run-B.md`:

- **Количество токенов** (видно в выводе Copilot CLI если включён verbose, или приблизительно по длине контекста)
- **Количество итераций** — сколько раз агент исправлял код после первой попытки
- **Успех компиляции** — `dotnet build BenchmarkExperiment` с первой попытки? Со второй?
- **Dry run** — `dotnet run -c Release --project BenchmarkExperiment -- --job Dry --filter "*" > dry-run.log 2>&1` успешно?

## Чеклист качества (заполни для каждого запуска)

Открой написанный код и отметь каждый пункт:

| # | Критерий | Запуск A | Запуск B |
|---|---|---|---|
| 1 | `dotnet add package BenchmarkDotNet` без версии (не хардкодит номер) | ☐ | ☐ |
| 2 | Benchmark-методы **возвращают** результат (нет void) | ☐ | ☐ |
| 3 | Инициализация в `[GlobalSetup]`, не в теле метода | ☐ | ☐ |
| 4 | `BenchmarkSwitcher.FromAssembly(typeof(T).Assembly).Run(args)` — args передан | ☐ | ☐ |
| 5 | Запуск через `dotnet run -c Release` (не Debug) | ☐ | ☐ |
| 6 | `[MemoryDiagnoser]` добавлен (или обоснованно не добавлен) | ☐ | ☐ |
| 7 | `[Benchmark(Baseline = true)]` есть хотя бы один | ☐ | ☐ |
| 8 | Нет ручных loops внутри benchmark-метода | ☐ | ☐ |
| 9 | `--job Dry` запускается без исключений | ☐ | ☐ |
| 10 | Комментарий `--filter` в коде или README | ☐ | ☐ |

**Итог**: `__/10` vs `__/10`

## Почему именно BenchmarkDotNet

- Это зона где модели предсказуемо ошибаются (устаревшие паттерны из training data)
- Результат объективно верифицируем: компилируется, запускается, чеклист
- `VectorDataExample` в этом репо — хороший объект для бенчмарка: есть `SimpleKeywordEmbeddingGenerator`
- Скилл имеет 13KB специфических инструкций + reference-файлы с актуальной документацией

## Ожидаемый результат

Если скилл работает как заявлено:
- Запуск B должен набрать 8-10/10 по чеклисту
- Запуск A вероятно 4-6/10 (void-методы, нет args, хардкод версии пакета)
- Запуск B должен пройти `--job Dry` с первой попытки

Если разницы нет — это тоже ценный результат для критической статьи.
