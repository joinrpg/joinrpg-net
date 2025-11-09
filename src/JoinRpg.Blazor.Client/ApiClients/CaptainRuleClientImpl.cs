using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class CaptainRuleClientImpl(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<CaptainRuleClientImpl> logger) : ICaptainRuleClient
{
    public async Task<CaptainRuleListViewModel> GetList(ProjectIdentification projectId)
    {
        return await httpClient.GetFromJsonAsync<CaptainRuleListViewModel>(
             $"webapi/captain-rule/getlist?projectId={projectId.Value}")
             ?? throw new Exception("Couldn't get result from server");
    }
    public async Task Remove(CaptainAccessRule rule)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/captain-rule/remove?projectId={rule.ProjectId.Value}", rule);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
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
            return await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<CaptainRuleListViewModel>()
                ?? throw new Exception("Empty");

        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }
}
