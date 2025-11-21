using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CharacterGroupsClient(HttpClient httpClient) : ICharacterGroupsClient
{
    public async Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterGroupDto>>(
            $"/webapi/character-groups/GetRealCharacterGroups?projectId={projectId}")
            ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterGroupDto>>(
            $"/webapi/character-groups/GetCharacterGroupsWithSpecial?projectId={projectId}")
            ?? throw new Exception("Couldn't get result from server");
    }

}
