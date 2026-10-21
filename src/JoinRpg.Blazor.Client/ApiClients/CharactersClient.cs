using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CharactersClient(HttpClient httpClient) : ICharactersClient
{
    public async Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType)
        => await (await httpClient.GetAsync($"/webapi/characters/GetCharactersByType?projectId={projectId}&listType={listType}"))
            .ReadFromJsonOrThrowProblem<List<CharacterDto>>();

    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId)
        => await (await httpClient.GetAsync($"/webapi/character-groups/GetCharacterGroups?projectId={projectId}"))
            .ReadFromJsonOrThrowProblem<List<CharacterGroupDto>>();
}
