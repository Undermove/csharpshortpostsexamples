using System.Threading.RateLimiting;
using MySqlConnector;

namespace RateLimitingExample;

public sealed class MySqlFixedWindowRateLimiterOptions
{
    public required string ConnectionString { get; set; }

    // Максимальное количество запросов в одном окне (по умолчанию 100)
    public int PermitLimit { get; set; } = 100;

    // Длительность одного окна (по умолчанию 1 минута)
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

public sealed class MySqlFixedWindowRateLimiter(
    string partitionKey, // к примеру ip адрес или имя клиента в общем что-то по чему можно идентифицировать пользователя
    MySqlFixedWindowRateLimiterOptions options)
    : RateLimiter
{
    // Время последнего обращения к лимитеру (монотонные часы в мс).
    // Environment.TickCount64 — миллисекунды с момента старта системы,
    // не зависит от перевода системных часов.
    private long _lastActivity = Environment.TickCount64;

    // Сколько времени лимитер простаивает без запросов.
    // Фреймворк использует это, чтобы выбросить неиспользуемые лимитеры из пула.
    // Interlocked.Read нужен, потому что на 32-битных платформах long (8 байт)
    // не читается атомарно — без него можно получить "порванное" значение.
    public override TimeSpan? IdleDuration =>
        TimeSpan.FromMilliseconds(Environment.TickCount64 - Interlocked.Read(ref _lastActivity));

    // Статистику не реализуем — возвращаем null
    public override RateLimiterStatistics? GetStatistics() => null;

    // Синхронная попытка получить разрешение — всегда отказ,
    // потому что для проверки нужно лезть в MySQL, а это async-операция.
    // Заставляет потребителей использовать AcquireAsync.
    protected override RateLimitLease AttemptAcquireCore(int permitCount) =>
        new MySqlRateLimitLease(false, null);

    // Основная логика — асинхронная проверка и инкремент счётчика в MySQL
    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount, CancellationToken cancellationToken)
    {
        // Атомарно обновляем время последней активности (парное к Interlocked.Read в IdleDuration)
        Interlocked.Exchange(ref _lastActivity, Environment.TickCount64);

        var windowSeconds = (int)options.Window.TotalSeconds;
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Вычисляем уникальный ID окна через целочисленное деление:
        // unix_time / 60 * 60 округляет вниз до начала текущей минуты.
        // Например: 1720000093 / 60 * 60 = 1720000080 → "w_1720000080"
        // Все запросы внутри одной минуты получат одинаковый windowId.
        var windowId = $"w_{nowUnix / windowSeconds * windowSeconds}";

        // Когда текущее окно истечёт:
        // % windowSeconds — сколько секунд уже прошло с начала окна,
        // вычитаем из размера окна — получаем сколько осталось.
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(windowSeconds - nowUnix % windowSeconds);

        await using var conn = new MySqlConnection(options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        // Атомарный upsert с трюком LAST_INSERT_ID(expr):
        // - Если записи нет → INSERT создаёт её со счётчиком 1.
        // - Если запись есть (ON DUPLICATE KEY UPDATE):
        //   - Если окно протухло (expires_at <= NOW(3)) → сбрасываем счётчик на 1
        //   - Иначе → инкрементим request_count + 1
        //   NOW(3) — текущее время MySQL с точностью до миллисекунд.
        // LAST_INSERT_ID(expr) "протаскивает" новое значение счётчика в сессионную
        // переменную — её потом можно прочитать через SELECT LAST_INSERT_ID().
        // Это наше собственное значение, никто другой его не перезапишет в рамках
        // нашей сессии — гонки между UPSERT и чтением больше нет.
        await using var upsertCmd = new MySqlCommand(
            """
            INSERT INTO rate_limit_counters (partition_key, window_id, request_count, expires_at)
            VALUES (@key, @windowId, 1, @expiresAt)
            ON DUPLICATE KEY UPDATE
                request_count =  (IF(expires_at <= NOW(3), 1, request_count + 1)),
                expires_at    = IF(expires_at <= NOW(3), @expiresAt, expires_at)
            """, conn);
        upsertCmd.Parameters.AddWithValue("@key", partitionKey);
        upsertCmd.Parameters.AddWithValue("@windowId", windowId);
        upsertCmd.Parameters.AddWithValue("@expiresAt", expiresAt.UtcDateTime);

        // Для INSERT ON DUPLICATE KEY UPDATE MySQL возвращает:
        //   1 — вставили новую строку
        //   2 — обновили существующую
        //   0 — совпала, но ничего не изменилось
        var rowsAffected = await upsertCmd.ExecuteNonQueryAsync(cancellationToken);

        long currentCount;
        if (rowsAffected == 1)
        {
            // Новая строка — значит наш запрос первый в окне, счётчик = 1.
            // В этой ветке LAST_INSERT_ID() в UPDATE не выполнялся, запрашивать нечего.
            currentCount = 1;
        }
        else
        {
            // Сработал UPDATE — читаем наше новое значение из сессионной переменной.
            // Это именно то число, которое мы только что записали, не чужое.
            await using var getCountCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", conn);
            currentCount = Convert.ToInt64(await getCountCmd.ExecuteScalarAsync(cancellationToken));
        }

        // Если лимит превышен — отказ с retryAfter.
        // expiresAt мы посчитали локально по границе окна — все инстансы для одного
        // окна получают одинаковое значение, поэтому читать его из БД не нужно.
        if (currentCount > options.PermitLimit)
        {
            var retryAfter = expiresAt - DateTimeOffset.UtcNow;
            return new MySqlRateLimitLease(false,
                retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1));
        }

        // Лимит не превышен — разрешаем запрос
        return new MySqlRateLimitLease(true, null);
    }

    // "Квитанция" — результат попытки получить разрешение на запрос.
    // Реализует стандартный контракт RateLimitLease из System.Threading.RateLimiting.
    private sealed class MySqlRateLimitLease(bool isAcquired, TimeSpan? retryAfter) : RateLimitLease
    {
        // Получил ли клиент разрешение на запрос
        public override bool IsAcquired { get; } = isAcquired;

        // Список доступных metadata-ключей.
        // Если есть retryAfter — сообщаем, что доступна метадата RetryAfter,
        // чтобы middleware мог вернуть клиенту заголовок Retry-After.
        public override IEnumerable<string> MetadataNames =>
            retryAfter.HasValue ? [MetadataName.RetryAfter.Name] : [];

        // Стандартный механизм получения metadata по ключу
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter.Name && retryAfter.HasValue)
            {
                metadata = retryAfter.Value;
                return true;
            }
            metadata = null;
            return false;
        }
    }
}
