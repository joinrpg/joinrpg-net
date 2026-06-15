using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

public class CharacterListViewService(
    ICharacterRepository characterRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor) : ICharactersClient
{
    public async Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType)
    {
        IEnumerable<Character> characters = listType switch
        {
            CharacterListType.All => await characterRepository.GetAllCharacters(projectId),
            CharacterListType.AllTemplates => await characterRepository.GetActiveTemplateCharacters(projectId),
            CharacterListType.Available => await characterRepository.GetAvailableCharacters(projectId),
            CharacterListType.AvailableNonSlots => await characterRepository.GetAvailableNonSlotCharacters(projectId),
            _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null)
        };
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var masterAccess = project.HasMasterAccess(currentUserAccessor);
        return [.. characters
            .Select(CreateDto)
            .Where(x => masterAccess || x.IsPublic)];
    }

    private static CharacterDto CreateDto(Character c) => new(
        new CharacterIdentification(c.ProjectId, c.CharacterId),
        c.CharacterName,
        LimitDescription(((MarkdownString?)c.Description).ToPlainTextAndEscapeHtml().ToString()),
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
