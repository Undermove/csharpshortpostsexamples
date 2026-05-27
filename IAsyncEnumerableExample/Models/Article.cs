namespace IAsyncEnumerableExample.Models;

// Статья базы знаний. Тело лежит как JSON-строка в LONGTEXT-колонке —
// ровно как в реальной БЗ: одна статья ~234 КБ.
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string ContentJson { get; set; } = "";   // ~234 КБ JSON в LONGTEXT
    public byte[] ContentBlob { get; set; } = [];    // те же байты в LONGBLOB — для честного стрима
}
