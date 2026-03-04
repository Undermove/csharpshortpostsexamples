using Microsoft.Extensions.AI;

namespace VectorDataExample;

/// <summary>
/// Простой эмбеддинг-генератор на основе ключевых слов — без API ключей.
/// В реальном проекте замените на OpenAI/Azure/Ollama через Microsoft.Extensions.AI.
/// </summary>
public class SimpleKeywordEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    // 32 ключевых слова-стемы = 32 измерения вектора
    private static readonly string[] Vocabulary =
    [
        "пароль", "сброс", "восстанов", "забыл", "аккаунт", "вход", "логин",
        "двухфактор", "2fa", "totp", "authenticator", "защит", "код", "безопасност",
        "email", "почт", "адрес", "измен", "смен", "регистрац",
        "оплат", "карт", "деньг", "банк", "заказ", "средств",
        "возврат", "отмен", "проблем", "ошибк", "помощ", "поддержк"
    ];

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = values
            .Select(text => new Embedding<float>(Vectorize(text)))
            .ToList();

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    private static float[] Vectorize(string text)
    {
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var vector = new float[Vocabulary.Length];

        for (int i = 0; i < Vocabulary.Length; i++)
        {
            // Матчим по стемам (начало слова)
            vector[i] = words.Count(w => w.StartsWith(Vocabulary[i]) || Vocabulary[i].StartsWith(w));
        }

        // Нормализуем вектор (L2)
        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= magnitude;
        else
            // Если слов из словаря нет — равномерный вектор
            Array.Fill(vector, 1f / MathF.Sqrt(vector.Length));

        return vector;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
