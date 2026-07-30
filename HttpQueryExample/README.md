# HttpQueryExample

Демо нового HTTP-метода QUERY (RFC 10008) на .NET 10: поиск по каталогу, где фильтр уезжает телом JSON, а не query string'ом.

## Запуск

```bash
dotnet run
```

и открыть http://localhost:5177 (порт из ASPNETCORE_URLS или launchSettings; по умолчанию `dotnet run` напишет свой).

## Что показывает

- **Бэкенд**: `MapQuery` extension (три строки поверх `MapMethods`), один хендлер на QUERY и POST, чтение фильтра через `ReadFromJsonAsync`.
- **Фронтенд** (React без сборки, `wwwroot/index.html`): `fetch(..., { method: 'QUERY' })` с fallback'ом на POST — при 405 или сетевой ошибке запрос повторяется POST'ом, и это запоминается на сессию.
- **Симуляция злого прокси**: тумблер в UI добавляет заголовок `X-Simulate-Waf`, middleware отвечает 405 на QUERY — можно вживую посмотреть, как отрабатывает fallback (лог запросов справа).

## Руками

```bash
# QUERY — работает
curl -X QUERY http://localhost:5177/products/search \
  -H 'Content-Type: application/json' \
  -d '{"categories":["мыши"],"maxPrice":120}'

# QUERY через «злой прокси» — 405
curl -X QUERY http://localhost:5177/products/search \
  -H 'Content-Type: application/json' -H 'X-Simulate-Waf: 1' -d '{}'

# POST fallback — тот же эндпоинт
curl -X POST http://localhost:5177/products/search \
  -H 'Content-Type: application/json' -d '{"maxPrice":150}'
```
