using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ResponsibleMasterRuleClient(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<ResponsibleMasterRuleClient> logger) : IResponsibleMasterRuleClient
{
    public async Task<ResponsibleMasterRuleListViewModel> GetResponsibleMasterRuleList(ProjectIdentification projectId)
    {
        return await (await httpClient.GetAsync($"webapi/resp-master-rule/getlist?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<ResponsibleMasterRuleListViewModel>();
    }
    public async Task RemoveResponsibleMasterRule(ProjectIdentification projectId, int ruleId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsync($"webapi/resp-master-rule/remove?projectId={projectId.Value}&ruleId={ruleId}", null);
            await response.EnsureSuccessOrThrowProblem();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<ResponsibleMasterRuleListViewModel> AddResponsibleMasterRule(ProjectIdentification projectId, int groupId, int masterId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsync($"webapi/resp-master-rule/add?projectId={projectId.Value}&groupId={groupId}&masterId={masterId}", null);
            return await response.ReadFromJsonOrThrowProblem<ResponsibleMasterRuleListViewModel>();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }
}
