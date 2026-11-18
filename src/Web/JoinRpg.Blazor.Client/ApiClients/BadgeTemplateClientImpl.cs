using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Web.ProjectMasterTools.Badges;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class BadgeTemplateClientImpl(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<BadgeTemplateClientImpl> logger) : IBadgeTemplateClient
{
    public async Task<BadgeTemplateListViewModel> GetList(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/badge-template/getlist?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<BadgeTemplateListViewModel>();

    public async Task<EditBadgeTemplateViewModel> GetById(ProjectBadgeTemplateIdentification id)
        => await (await httpClient.GetAsync($"webapi/badge-template/getbyid?{Query(id)}"))
            .ReadFromJsonOrThrowProblem<EditBadgeTemplateViewModel>();

    public async Task<ProjectBadgeTemplateIdentification> Create(ProjectIdentification projectId, EditBadgeTemplateViewModel model)
        => await Post(
            $"webapi/badge-template/create?projectId={projectId.Value}",
            model,
            response => response.ReadFromJsonOrThrowProblem<ProjectBadgeTemplateIdentification>());

    public Task Update(ProjectBadgeTemplateIdentification id, EditBadgeTemplateViewModel model)
        => Post($"webapi/badge-template/update?{Query(id)}", model, EnsureSuccess);

    public Task Remove(ProjectBadgeTemplateIdentification id)
        => Post<object?, bool>($"webapi/badge-template/remove?{Query(id)}", null, EnsureSuccess);

    public Task Restore(ProjectBadgeTemplateIdentification id)
        => Post<object?, bool>($"webapi/badge-template/restore?{Query(id)}", null, EnsureSuccess);

    private static string Query(ProjectBadgeTemplateIdentification id)
        => $"projectId={id.ProjectId.Value}&badgeTemplateId={id.ProjectBadgeTemplateId}";

    private static async Task<bool> EnsureSuccess(HttpResponseMessage response)
    {
        await response.EnsureSuccessOrThrowProblem();
        return true;
    }

    private async Task<TResult> Post<TBody, TResult>(string url, TBody body, Func<HttpResponseMessage, Task<TResult>> read)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync(url, body);
            return await read(response);
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Ошибка запроса к шаблонам бейджей {Url}", url);
            throw;
        }
    }
}
