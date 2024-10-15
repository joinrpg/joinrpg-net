using System.Net.Http.Json;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CharactersClient(HttpClient httpClient) : ICharactersClient
{
    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterGroupDto>>(
            $"/webapi/character-groups/GetCharacterGroups?projectId={projectId}")
            ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<List<CharacterDto>> GetCharacters(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterDto>>(
        $"/webapi/character-groups/GetCharacters?projectId={projectId}")
        ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<List<CharacterDto>> GetTemplateCharacters(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<CharacterDto>>(
        $"/webapi/character-groups/GetTempalteCharacters?projectId={projectId}")
        ?? throw new Exception("Couldn't get result from server");
    }
}
