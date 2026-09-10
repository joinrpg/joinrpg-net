using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
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
            CharacterListType.AvailableForMaster => await characterRepository.GetAvailableCharacters(projectId),
            CharacterListType.AvailableNonSlotsForMaster => await characterRepository.GetAvailableNonSlotCharacters(projectId),
            CharacterListType.AvailableTemplatesForMaster => await characterRepository.GetAvailableTemplateCharacters(projectId),
            _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null)
        };
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);

        if (listType.RequiresAvailabilityCheck())
        {
            // SQL-предикат (CharacterPredicates.IsAvailable) — только грубый префильтр: он не знает
            // ни про лимит слота, ни про статус проекта. Точный ответ даёт общий движок правил.
            //
            // AddByMaster, а не DisplayForPlayer: все потребители этих списков — мастерские
            // операции, и мастер вправе заявлять в проект с закрытым приёмом заявок. С
            // DisplayForPlayer фатальный ProjectClaimsClosed вычистил бы весь список.
            //
            // ВАЖНО: при userInfo: null правила читают только скалярные поля уже загруженной
            // сущности. Если в них появится обращение к character.Claims или character.Project
            // вне ветки "известен игрок", здесь будет ленивая подгрузка на каждого персонажа.
            characters = characters.Where(character =>
                character.ValidateIfCanAddClaim(userInfo: null, project, ClaimOperation.AddByMaster).Count == 0);
        }

        var masterAccess = project.HasMasterAccess(currentUserAccessor);
        return [.. characters
            .Select(CreateDto)
            .Where(x => masterAccess || x.IsPublic)
            .OrderBy(x => x.Name)];
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
