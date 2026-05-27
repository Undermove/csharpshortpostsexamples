using System.Text;
using System.Text.Json;
using IAsyncEnumerableExample.Models;
using Microsoft.EntityFrameworkCore;

namespace IAsyncEnumerableExample.Data;

public static class DbSeeder
{
    // Генерим N статей по ~approxSizeBytes байт, чтобы разница «буферизация vs стриминг»
    // была видна невооружённым глазом.
    public static async Task SeedAsync(KnowledgeBaseContext db, int count, int approxSizeBytes)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Articles.AnyAsync()) return;

        for (var i = 1; i <= count; i++)
        {
            var json = BuildArticleJson(i, approxSizeBytes);
            db.Articles.Add(new Article
            {
                Title = $"Article #{i}",
                ContentJson = json,
                ContentBlob = Encoding.UTF8.GetBytes(json)   // те же байты, но бинарём
            });

            // Сохраняем пачками и чистим трекер, чтобы сам сидинг не съел всю память.
            if (i % 50 == 0)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }
        }

        await db.SaveChangesAsync();
    }

    private static string BuildArticleJson(int id, int approxSizeBytes)
    {
        // ASCII-наполнитель: 1 символ = 1 байт, чтобы размер статьи был честные ~234 КБ
        // и на диске, и в ответе (без раздувания \uXXXX-эскейпами).
        var sb = new StringBuilder(approxSizeBytes);
        const string chunk = "Knowledge base article body filler text for streaming demo. ";
        while (sb.Length < approxSizeBytes) sb.Append(chunk);

        var article = new
        {
            id,
            title = $"Article #{id}",
            tags = new[] { "knowledge-base", "demo", "streaming" },
            body = sb.ToString()
        };
        return JsonSerializer.Serialize(article);
    }
}
