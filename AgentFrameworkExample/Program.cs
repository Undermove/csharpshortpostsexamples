// Agent Framework — AI-агент для работы с GitHub
// Тот же функционал что и SemanticKernelExample, но на новом фреймворке.
// Сравните: меньше обвязки, нет Kernel, нет KernelPlugin, нет KernelFunction.
//
// Запуск:  dotnet run
//
// Переменные окружения:
//   OPENAI_API_KEY  — ключ OpenAI
//   GITHUB_TOKEN    — Personal Access Token с правами repo

using AgentFrameworkExample;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

Env.TraversePath().Load();

var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Укажите переменную окружения OPENAI_API_KEY");

var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
    ?? throw new InvalidOperationException("Укажите переменную окружения GITHUB_TOKEN");

// --- Создаём агента ---
var chatClient = new OpenAIClient(openAiKey)
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

var githubTools = new GitHubTools(githubToken);

AIAgent agent = chatClient.AsAIAgent(
    name: "GitHubAgent",
    instructions: """
        Ты AI-ассистент для работы с GitHub репозиториями.
        У тебя есть инструменты: читать файлы, создавать ветки, обновлять файлы, создавать PR.

        Когда тебя просят отредактировать файл и создать PR — действуй по шагам:
        1. Прочитай текущее содержимое файла
        2. Примени нужные изменения
        3. Создай новую ветку
        4. Запиши изменённый файл в новую ветку
        5. Создай PR с понятным описанием что именно изменилось

        Всегда сообщай пользователю что ты делаешь на каждом шаге.
        """,
    tools: githubTools.AsTools());

// --- Создаём сессию один раз — она хранит историю переписки ---
var session = await agent.CreateSessionAsync();

Console.WriteLine("🤖 GitHub Agent запущен (Agent Framework). Что нужно сделать?");
Console.WriteLine();
Console.WriteLine("💡 Примеры:");
Console.WriteLine("   • Прочитай файл README.md в репозитории owner/repo");
Console.WriteLine("   • Отредактируй README.md — добавь раздел Installation — создай PR");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input)) continue;
    if (input is "выход" or "exit" or "quit") break;

    Console.WriteLine();
    try
    {
        // Одна строчка — агент сам крутит луп с тулами
        var response = await agent.RunAsync(input, session, null);
        Console.WriteLine($"🤖 {response.Text}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка: {ex.Message}");
    }

    Console.WriteLine();
}
