// Semantic Kernel — AI-агент для работы с GitHub
// Агент сам решает какие инструменты вызывать и в каком порядке.
//
// Запуск локально:  dotnet run
// Запуск в Docker:  docker-compose up
//
// Переменные окружения:
//   OPENAI_API_KEY  — ключ OpenAI
//   GITHUB_TOKEN    — Personal Access Token с правами repo

using DotNetEnv;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelExample;

// Загружает .env если он есть (для локального запуска и дебага)
// В Docker переменные окружения передаются через docker-compose.yml
Env.TraversePath().Load();

var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Укажите переменную окружения OPENAI_API_KEY");

var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
    ?? throw new InvalidOperationException("Укажите переменную окружения GITHUB_TOKEN");

// --- Настройка SK ---
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-5-mini", openAiKey)
    .Build();

// Регистрируем плагин — SK через рефлексию читает [KernelFunction] методы
// и превращает их в инструменты для GPT (OpenAI Function Calling)
kernel.Plugins.AddFromObject(new GitHubPlugin(githubToken), "GitHub");

var chat = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory("""
    Ты AI-ассистент для работы с GitHub репозиториями.
    У тебя есть инструменты: читать файлы, создавать ветки, обновлять файлы, создавать PR.
    
    Когда тебя просят отредактировать файл и создать PR — действуй по шагам:
    1. Прочитай текущее содержимое файла
    2. Примени нужные изменения
    3. Создай новую ветку
    4. Запиши изменённый файл в новую ветку
    5. Создай PR с понятным описанием что именно изменилось
    
    Всегда сообщай пользователю что ты делаешь на каждом шаге.
    """);

// FunctionChoiceBehavior.Auto() — SK автоматически вызывает функции
// когда GPT решает что они нужны, и передаёт результат обратно в контекст
var settings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

Console.WriteLine("🤖 GitHub Agent запущен. Что нужно сделать?");
Console.WriteLine();
Console.WriteLine("💡 Примеры:");
Console.WriteLine("   • Прочитай файл README.md в репозитории owner/repo");
Console.WriteLine("   • Отредактируй README.md в owner/repo — добавь раздел ## Installation с командой dotnet run — создай PR с веткой feature/add-installation");
Console.WriteLine("   • Создай ветку fix/typo в репозитории owner/repo от main");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input)) continue;
    if (input is "выход" or "exit" or "quit") break;

    history.AddUserMessage(input);

    Console.WriteLine();
    try
    {
        // SK запускает агентный луп:
        // GPT видит инструменты → вызывает нужные → получает результат → решает что дальше
        var response = await chat.GetChatMessageContentAsync(history, settings, kernel);
        history.AddAssistantMessage(response.Content ?? "");
        Console.WriteLine($"🤖 {response.Content}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка: {ex.Message}");
    }

    Console.WriteLine();
}
