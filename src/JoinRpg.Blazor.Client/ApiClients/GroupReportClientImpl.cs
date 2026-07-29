using System.Net;
using JoinRpg.Web.CharacterGroups.GroupReport;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class GroupReportClientImpl(HttpClient httpClient) : IGroupReportClient
{
    public async Task<GroupReportViewModel?> GetReport(CharacterGroupIdentification groupId)
    {
        var response = await httpClient.GetAsync(
            $"webapi/group-report/get?projectId={groupId.ProjectId.Value}&characterGroupId={groupId.CharacterGroupId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<GroupReportViewModel>();
    }
}
