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

    /// <summary>
    /// Версия для пути записи: ProjectInfo приходит снаружи, а не из кеша. Иначе нарушится
    /// инвариант CharacterInfo о единственном экземпляре ProjectInfo (ADR013).
    /// </summary>
    Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId, ProjectInfo projectInfo);

    Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(IReadOnlyCollection<CharacterIdentification> characterIds);

    /// <summary>
    /// Персонажи, лежащие непосредственно в любой из указанных групп. Раскрытие дерева групп —
    /// на стороне вызывающего (<c>ProjectInfo.GetChildGroupIdsIncludingThis</c>).
    /// </summary>
    /// <param name="spec">
    /// Какие персонажи нужны. По умолчанию все: собирать агрегат (поля, заявки) на удалённых,
    /// которые вызывающему не нужны, — лишняя работа, поэтому спискам стоит просить
    /// <see cref="CharacterStatusSpec.Active"/> явно.
    /// </param>
    Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(
        ProjectIdentification projectId,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds,
        CharacterStatusSpec spec = CharacterStatusSpec.Any);

    /// <summary>Все персонажи проекта — по умолчанию включая удалённых.</summary>
    Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(
        ProjectIdentification projectId,
        CharacterStatusSpec spec = CharacterStatusSpec.Any);

    async Task<CharacterInfo> GetCharacterInfo(CharacterIdentification characterId)
        => await GetCharacterInfoOrDefault(characterId)
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

    /// <summary>
    /// Персонажи проекта в виде лёгкой проекции для списков выбора.
    /// </summary>
    /// <remarks>
    /// Отдельно от <see cref="GetAllCharacterInfos"/>: спискам не нужны ни поля, ни финансы, ни
    /// комментарии, а агрегат тянет их на каждого персонажа. Фильтрация — на стороне вызывающего,
    /// доменными правилами: отдельного SQL-предиката «доступен для заявки» тут сознательно нет,
    /// иначе он разъедется с правилами (так уже было, см. issue #4766).
    /// </remarks>
    Task<IReadOnlyCollection<CharacterListEntry>> GetCharactersForList(
        ProjectIdentification projectId,
        CharacterStatusSpec spec = CharacterStatusSpec.Any);
}
