using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.AI;
using VectorDataExample;

namespace BenchmarkExperiment;

[MemoryDiagnoser]
public class EmbeddingGeneratorBenchmarks
{
    private SimpleKeywordEmbeddingGenerator _generator = null!;

    private static readonly IEnumerable<string> SingleInput =
    [
        "забыл пароль от аккаунта"
    ];

    private static readonly IEnumerable<string> TenInputs =
    [
        "забыл пароль от аккаунта",
        "как сбросить пароль",
        "проблема с двухфакторной аутентификацией",
        "не могу войти в систему",
        "восстановление доступа к email",
        "изменить адрес электронной почты",
        "оплата картой не проходит",
        "возврат средств за заказ",
        "отмена заказа",
        "ошибка при регистрации"
    ];

    [GlobalSetup]
    public void Setup()
    {
        _generator = new SimpleKeywordEmbeddingGenerator();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _generator.Dispose();
    }

    [Benchmark(Baseline = true, Description = "1 строка")]
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateSingle()
        => _generator.GenerateAsync(SingleInput);

    [Benchmark(Description = "10 строк")]
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateTen()
        => _generator.GenerateAsync(TenInputs);
}
