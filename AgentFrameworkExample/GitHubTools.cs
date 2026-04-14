using System.ComponentModel;
using Microsoft.Extensions.AI;
using Octokit;

namespace AgentFrameworkExample;

/// <summary>
/// Инструменты для работы с GitHub.
/// В Agent Framework не нужен [KernelFunction] — достаточно [Description].
/// AIFunctionFactory.Create() сам превратит методы в тулы для LLM.
/// </summary>
public class GitHubTools
{
    private readonly GitHubClient _client;

    public GitHubTools(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("AgentFrameworkExample"))
        {
            Credentials = new Credentials(token)
        };
    }

    [Description("Читает содержимое файла из GitHub репозитория")]
    public async Task<string> GetFileContent(
        [Description("Владелец репозитория")] string owner,
        [Description("Название репозитория")] string repo,
        [Description("Путь к файлу")] string path,
        [Description("Ветка, по умолчанию main")] string branch = "main")
    {
        var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);
        return contents[0].Content ?? "";
    }

    [Description("Создаёт новую ветку в репозитории")]
    public async Task<string> CreateBranch(
        [Description("Владелец репозитория")] string owner,
        [Description("Название репозитория")] string repo,
        [Description("Название новой ветки")] string branchName,
        [Description("Базовая ветка")] string baseBranch = "main")
    {
        var baseRef = await _client.Git.Reference.Get(owner, repo, $"refs/heads/{baseBranch}");
        await _client.Git.Reference.Create(owner, repo,
            new NewReference($"refs/heads/{branchName}", baseRef.Object.Sha));
        return $"Ветка '{branchName}' создана от '{baseBranch}'";
    }

    [Description("Обновляет содержимое файла и создаёт коммит")]
    public async Task<string> UpdateFile(
        [Description("Владелец репозитория")] string owner,
        [Description("Название репозитория")] string repo,
        [Description("Путь к файлу")] string path,
        [Description("Новое содержимое файла")] string content,
        [Description("Сообщение коммита")] string commitMessage,
        [Description("Ветка для коммита")] string branch)
    {
        var existing = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);
        await _client.Repository.Content.UpdateFile(owner, repo, path,
            new UpdateFileRequest(commitMessage, content, existing[0].Sha, branch));
        return $"Файл '{path}' обновлён в ветке '{branch}'";
    }

    [Description("Создаёт pull request в репозитории")]
    public async Task<string> CreatePullRequest(
        [Description("Владелец репозитория")] string owner,
        [Description("Название репозитория")] string repo,
        [Description("Заголовок pull request")] string title,
        [Description("Описание pull request")] string body,
        [Description("Ветка с изменениями")] string headBranch,
        [Description("Целевая ветка")] string baseBranch = "main")
    {
        var pr = await _client.PullRequest.Create(owner, repo,
            new NewPullRequest(title, headBranch, baseBranch) { Body = body });
        return $"PR создан: {pr.HtmlUrl}";
    }

    /// <summary>
    /// Возвращает все методы как AIFunction для регистрации в агенте.
    /// </summary>
    public IList<AITool> AsTools()
    {
        return
        [
            AIFunctionFactory.Create(GetFileContent, name: "GetFileContent"),
            AIFunctionFactory.Create(CreateBranch, name: "CreateBranch"),
            AIFunctionFactory.Create(UpdateFile, name: "UpdateFile"),
            AIFunctionFactory.Create(CreatePullRequest, name: "CreatePullRequest"),
        ];
    }
}
