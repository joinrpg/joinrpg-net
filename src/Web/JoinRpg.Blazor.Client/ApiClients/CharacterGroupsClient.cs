using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CharacterGroupsClient(HttpClient httpClient) : ICharacterGroupsClient
{
    public async Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId)
        => await (await httpClient.GetAsync($"/webapi/character-groups/GetRealCharacterGroups?projectId={projectId}"))
            .ReadFromJsonOrThrowProblem<List<CharacterGroupDto>>();

    public async Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId)
        => await (await httpClient.GetAsync($"/webapi/character-groups/GetCharacterGroupsWithSpecial?projectId={projectId}"))
            .ReadFromJsonOrThrowProblem<List<CharacterGroupDto>>();

    public async Task<List<CharacterGroupDto>> GetValidParentGroups(CharacterGroupIdentification groupId)
        => await (await httpClient.GetAsync($"/webapi/character-groups/GetValidParentGroups?projectId={groupId.ProjectId}&groupId={groupId}"))
            .ReadFromJsonOrThrowProblem<List<CharacterGroupDto>>();
}
