using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Data.Interfaces.Characters;

/// <summary>
/// Загрузка доменного агрегата персонажа (ADR013).
/// </summary>
/// <remarks>
/// Отдельный интерфейс, а не расширение <see cref="ICharacterRepository"/>: тот отдаёт
/// EF-сущности <c>Character</c>, а его реализация прогревает контекст всем проектом целиком
/// (см. ADR011). Здесь этого нет — каждый метод делает ровно один запрос.
/// Заявки грузятся всегда все, включая отклонённые: <see cref="CharacterInfo"/> несёт полную
/// правду о персонаже, а фильтрация сломала бы его инварианты.
/// </remarks>
public interface ICharacterInfoRepository
{
    Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId);

    Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(IReadOnlyCollection<CharacterIdentification> characterIds);

    /// <summary>
    /// Персонажи, лежащие непосредственно в любой из указанных групп. Раскрытие дерева групп —
    /// на стороне вызывающего (<c>ProjectInfo.GetChildGroupIdsIncludingThis</c>).
    /// </summary>
    Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(
        ProjectIdentification projectId,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds);

    /// <summary>Все персонажи проекта, включая удалённых (<c>IsActive == false</c>).</summary>
    Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId);

    async Task<CharacterInfo> GetCharacterInfo(CharacterIdentification characterId)
        => await GetCharacterInfoOrDefault(characterId)
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");
}
