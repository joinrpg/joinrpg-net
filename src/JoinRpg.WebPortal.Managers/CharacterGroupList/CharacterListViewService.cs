using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

public class CharacterListViewService(ICharacterRepository characterRepository, IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : ICharactersClient
{
    public async Task<List<CharacterDto>> GetCharacters(int projectId)
    {
        var characters = await characterRepository.GetAllCharacters(projectId);
        return await ToFilteredList(characters, projectId);
    }

    public async Task<List<CharacterDto>> GetTemplateCharacters(int projectId)
    {
        var characters = await characterRepository.GetTemplateCharacters(projectId);
        return await ToFilteredList(characters, projectId);
    }

    private async Task<List<CharacterDto>> ToFilteredList(IEnumerable<Character> characters, int projectId)
    {
        var project = await projectRepository.GetProjectAsync(projectId);
        var masterAccess = project.HasMasterAccess(currentUserAccessor);
        return [.. characters
            .Select(CreateDto)
            .Where(x => masterAccess || x.IsPublic)];
    }

    private static CharacterDto CreateDto(Character c) => new(
        new PrimitiveTypes.CharacterIdentification(c.ProjectId, c.CharacterId),
        c.CharacterName,
        LimitDescription(c.Description.ToPlainText().ToString()),
        c.IsPublic);

    private static string LimitDescription(string v)
    {
        // TODO respect word boundaries
        if (v.Length < 100)
        {
            return v;
        }
        return v[0..100] + "...";

    }
}
