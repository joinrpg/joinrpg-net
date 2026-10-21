using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class CaptainRuleClientImpl(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<CaptainRuleClientImpl> logger) : ICaptainRuleClient
{
    public async Task<CaptainRuleListViewModel> GetList(ProjectIdentification projectId)
    {
        return await (await httpClient.GetAsync($"webapi/captain-rule/getlist?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<CaptainRuleListViewModel>();
    }
    public async Task Remove(CaptainAccessRule rule)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/captain-rule/remove?projectId={rule.ProjectId.Value}", rule);
            await response.EnsureSuccessOrThrowProblem();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<CaptainRuleListViewModel> AddOrChange(CaptainAccessRule rule)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/captain-rule/addorchange?projectId={rule.ProjectId.Value}", rule);
            return await response.ReadFromJsonOrThrowProblem<CaptainRuleListViewModel>();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }
}
