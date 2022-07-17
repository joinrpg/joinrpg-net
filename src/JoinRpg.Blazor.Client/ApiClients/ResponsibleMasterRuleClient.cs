using System.Net.Http.Json;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ResponsibleMasterRuleClient : IResponsibleMasterRuleClient
{
    private readonly HttpClient httpClient;
    private readonly CsrfTokenProvider csrfTokenProvider;
    private readonly ILogger<ResponsibleMasterRuleClient> logger;

    public ResponsibleMasterRuleClient(
        HttpClient httpClient,
        CsrfTokenProvider csrfTokenProvider,
        ILogger<ResponsibleMasterRuleClient> logger)
    {
        this.httpClient = httpClient;
        this.csrfTokenProvider = csrfTokenProvider;
        this.logger = logger;
    }

    public async Task<ResponsibleMasterRuleListViewModel> GetResponsibleMasterRuleList(ProjectIdentification projectId)
    {
        return await httpClient.GetFromJsonAsync<ResponsibleMasterRuleListViewModel>(
             $"webapi/resp-master-rule/getlist?projectId={projectId.Value}")
             ?? throw new Exception("Couldn't get result from server");
    }
    public async Task RemoveResponsibleMasterRule(ProjectIdentification projectId, int ruleId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            await httpClient.PostAsync($"webapi/resp-master-rule/remove?projectId={projectId.Value}&ruleId={ruleId}", null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task AddResponsibleMasterRule(ProjectIdentification projectId, int groupId, int masterId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            await httpClient.PostAsync($"webapi/resp-master-rule/add?projectId={projectId.Value}&groupId={groupId}&masterId={masterId}", null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }
}
