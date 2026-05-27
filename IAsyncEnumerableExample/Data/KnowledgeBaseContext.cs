using IAsyncEnumerableExample.Models;
using Microsoft.EntityFrameworkCore;

namespace IAsyncEnumerableExample.Data;

public class KnowledgeBaseContext(DbContextOptions<KnowledgeBaseContext> options)
    : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(e =>
        {
            e.ToTable("articles");
            e.HasKey(a => a.Id);
            e.Property(a => a.Title).HasMaxLength(512);
            // Без MaxLength Pomelo маппит string в LONGTEXT — то что нужно для больших JSON.
            e.Property(a => a.ContentJson).HasColumnType("LONGTEXT");
            // Те же данные в бинарной колонке — только её можно реально стримить через GetStream.
            e.Property(a => a.ContentBlob).HasColumnType("LONGBLOB");
        });
    }
}
