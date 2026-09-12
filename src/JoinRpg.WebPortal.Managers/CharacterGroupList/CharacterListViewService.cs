using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

public class CharacterListViewService(
    ICharacterInfoRepository characterInfoRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor) : ICharactersClient
{
    public async Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var characters = await characterInfoRepository.GetCharactersForList(projectId);

        var masterAccess = project.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault);

        return [.. characters
            .Where(character => Matches(character, listType, project))
            .Where(character => masterAccess || character.IsPublic)
            .Select(CreateDto)
            .OrderBy(x => x.Name)];
    }

    /// <summary>
    /// Подходит ли персонаж под запрошенный вид списка.
    /// </summary>
    /// <remarks>
    /// Доступность считается общим движком правил, а не отдельным SQL-предикатом: предикат уже
    /// один раз разъехался с правилами (не знал ни про лимит слота, ни про статус проекта), см.
    /// issue #4766. Проекция <see cref="CharacterListEntry"/> реализует <c>IClaimTarget</c>, так
    /// что правила применяются к ней напрямую.
    ///
    /// Операция — <see cref="ClaimOperation.AddByMaster"/>: все потребители списков <c>*ForMaster</c>
    /// мастерские, и мастер вправе заявлять в проект с закрытым приёмом заявок. С
    /// <c>DisplayForPlayer</c> фатальный <c>ProjectClaimsClosed</c> вычистил бы весь список.
    /// </remarks>
    private static bool Matches(CharacterListEntry character, CharacterListType listType, ProjectInfo project)
    {
        var isSlot = character.CharacterTypeInfo.CharacterType == CharacterType.Slot;

        return listType switch
        {
            CharacterListType.All => true,
            CharacterListType.AllTemplates => character.IsActive && isSlot,
            CharacterListType.AvailableForMaster => IsAvailableForMaster(character, project),
            CharacterListType.AvailableNonSlotsForMaster => !isSlot && IsAvailableForMaster(character, project),
            CharacterListType.AvailableTemplatesForMaster => isSlot && IsAvailableForMaster(character, project),
            _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null),
        };
    }

    private static bool IsAvailableForMaster(CharacterListEntry character, ProjectInfo project)
        => ClaimValidator
            .Validate(character, userInfo: null, movedClaim: null, project, ClaimOperation.AddByMaster)
            .Count == 0;

    private static CharacterDto CreateDto(CharacterListEntry c) => new(
        c.Id,
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
