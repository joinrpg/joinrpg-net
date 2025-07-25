using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CharacterGroupsClient(HttpClient httpClient) : ICharacterGroupsClient
{
    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterGroupDto>>(
            $"/webapi/character-groups/GetCharacterGroups?projectId={projectId}")
            ?? throw new Exception("Couldn't get result from server");
    }

}
