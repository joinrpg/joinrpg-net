namespace JoinRpg.Web.ProjectCommon;

public interface ICharactersClient
{
    Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType = CharacterListType.All);
}

/// <summary>
/// Какой набор персонажей показать в списке выбора.
/// </summary>
/// <remarks>
/// Варианты <c>*ForMaster</c> считаются от лица мастера: они включают персонажей в проекте с
/// закрытым приёмом заявок, потому что мастер вправе туда приглашать и переносить заявки. В
/// игроцком UI использовать их нельзя.
/// </remarks>
public enum CharacterListType
{
    All,
    AllTemplates,

    /// <summary>Куда мастер может создать или перенести заявку.</summary>
    AvailableForMaster,

    /// <summary>
    /// То же, но без слотов. Нужен для переноса утверждённой заявки: в слот её перенести нельзя,
    /// см. <c>AddClaimForbideReason.ApprovedClaimMovedToSlot</c>.
    /// </summary>
    AvailableNonSlotsForMaster,

    /// <summary>Только слоты, куда мастер может создать заявку.</summary>
    AvailableTemplatesForMaster,
}

public static class CharacterListTypeExtensions
{
    /// <summary>
    /// Списку нужен точный ответ доменных правил поверх грубого SQL-префильтра.
    /// </summary>
    public static bool RequiresAvailabilityCheck(this CharacterListType listType)
        => listType is CharacterListType.AvailableForMaster
            or CharacterListType.AvailableNonSlotsForMaster
            or CharacterListType.AvailableTemplatesForMaster;
}
