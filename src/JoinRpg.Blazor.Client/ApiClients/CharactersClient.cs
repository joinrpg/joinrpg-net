using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CharactersClient(HttpClient httpClient) : ICharactersClient
{
    public async Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterDto>>(
            $"/webapi/characters/GetCharactersByType?projectId={projectId}&listType={listType}")
            ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterGroupDto>>(
            $"/webapi/character-groups/GetCharacterGroups?projectId={projectId}")
            ?? throw new Exception("Couldn't get result from server");
    }
}
