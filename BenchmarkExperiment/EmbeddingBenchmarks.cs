using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.AI;
using VectorDataExample;

namespace BenchmarkExperiment;

[MemoryDiagnoser]
public class EmbeddingBenchmarks
{
    private SimpleKeywordEmbeddingGenerator _generator = null!;

    private static readonly IReadOnlyList<string> SingleInput =
    [
        "сбросить пароль от аккаунта"
    ];

    private static readonly IReadOnlyList<string> TenInputs =
    [
        "сбросить пароль от аккаунта",
        "проблема с двухфакторной аутентификацией",
        "забыл логин для входа",
        "восстановление доступа к аккаунту",
        "изменить адрес электронной почты",
        "ошибка при оплате картой",
        "возврат денег за заказ",
        "отмена заказа и возврат средств",
        "поддержка по вопросам безопасности",
        "помощь с регистрацией нового аккаунта"
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
    public Task<GeneratedEmbeddings<Embedding<float>>> SingleString()
        => _generator.GenerateAsync(SingleInput);

    [Benchmark(Description = "10 строк")]
    public Task<GeneratedEmbeddings<Embedding<float>>> TenStrings()
        => _generator.GenerateAsync(TenInputs);
}
