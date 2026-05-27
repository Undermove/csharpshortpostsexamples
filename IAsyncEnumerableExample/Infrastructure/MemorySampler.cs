namespace IAsyncEnumerableExample.Infrastructure;

// Фоновый семплер: каждые 25 мс смотрит на размер управляемой кучи и запоминает пик.
// Нужен, чтобы поймать момент, когда «буферная» ручка раздувает память на время запроса.
public class MemorySampler : BackgroundService
{
    private long _peakBytes;

    public long PeakBytes => Interlocked.Read(ref _peakBytes);

    public void Reset() => Interlocked.Exchange(ref _peakBytes, GC.GetTotalMemory(forceFullCollection: false));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var current = GC.GetTotalMemory(forceFullCollection: false);
            if (current > Interlocked.Read(ref _peakBytes))
                Interlocked.Exchange(ref _peakBytes, current);

            try { await Task.Delay(25, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
