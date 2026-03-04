using System.ComponentModel;
using Microsoft.SemanticKernel;
using Octokit;

namespace SemanticKernelExample;

/// <summary>
/// Инструменты для работы с GitHub — SK сам решает когда и какой вызвать.
/// </summary>
public class GitHubPlugin
{
    private readonly GitHubClient _client;

    public GitHubPlugin(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("SemanticKernelExample"))
        {
            Credentials = new Credentials(token)
        };
    }

    [KernelFunction, Description("Читает содержимое файла из GitHub репозитория")]
    public async Task<string> GetFileContent(
        [Description("Владелец репозитория (username или org)")] string owner,
        [Description("Название репозитория")] string repo,
        [Description("Путь к файлу, например README.md или src/Program.cs")] string path,
        [Description("Ветка, по умолчанию main")] string branch = "main")
    {
        var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);
        return contents[0].Content ?? "";
    }

    [KernelFunction, Description("Создаёт новую ветку в репозитории на основе существующей")]
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

    [KernelFunction, Description("Обновляет содержимое файла в репозитории и создаёт коммит")]
    public async Task<string> UpdateFile(
        [Description("Владелец репозитория")] string owner,
        [Description("Название репозитория")] string repo,
        [Description("Путь к файлу")] string path,
        [Description("Новое полное содержимое файла")] string content,
        [Description("Сообщение коммита")] string commitMessage,
        [Description("Ветка для коммита")] string branch)
    {
        var existing = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);
        await _client.Repository.Content.UpdateFile(owner, repo, path,
            new UpdateFileRequest(commitMessage, content, existing[0].Sha, branch));
        return $"Файл '{path}' обновлён в ветке '{branch}'";
    }

    [KernelFunction, Description("Создаёт pull request в репозитории")]
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
}
