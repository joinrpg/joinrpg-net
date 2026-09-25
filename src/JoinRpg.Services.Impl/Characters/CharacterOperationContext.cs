using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Services.Impl.Characters;

/// <summary>
/// База контекста доменной операции над персонажем: согласованное время операции, текущий
/// пользователь и снимок метаданных проекта.
/// </summary>
internal abstract record CharacterOperationContext(
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    FieldSaveHelper FieldSaveHelper)
{
    /// <summary>
    /// Тронула ли операция метаданные проекта. Сегодня единственный такой канал — отметка
    /// <see cref="ProjectField.WasEverUsed"/> при первом заполнении поля; её ставит
    /// <see cref="CharacterFields.FieldSaveHelper"/>, а флаг поднимает <c>SaveFields</c>. Если флаг
    /// поднят, сервис после сохранения пересоберёт <see cref="ProjectInfo"/> и обновит кэш — иначе
    /// следующая страница в том же запросе покажет поле неиспользованным.
    /// </summary>
    /// <remarks>
    /// Временное решение: по ADR014 отметка переедет в <c>ctx.MarkFieldsUsed</c>, и тогда у флага
    /// появится единственный явный писатель.
    /// </remarks>
    internal bool ProjectMetadataChanged { get; set; }

    /// <summary>
    /// Сохраняет значения полей персонажа и поднимает <see cref="ProjectMetadataChanged"/>, если
    /// операция впервые отметила поле или вариант как использованные.
    /// </summary>
    /// <param name="character">Персонаж, чьи поля сохраняются.</param>
    /// <param name="fieldsToSet">
    /// Дельта, а не полный слой. Пустой слой не означает «ничего не делать»: пересохранение пустым
    /// слоем нужно ради побочных эффектов — генерации значений по умолчанию, переноса значений
    /// заявка→персонаж и пересчёта спецгрупп.
    /// </param>
    /// <returns>Изменившиеся поля.</returns>
    protected IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveFieldsCore(
        Character character,
        FieldLayerContainer fieldsToSet)
    {
        var changed = FieldSaveHelper.SaveCharacterFields(CurrentUser.UserId, character, fieldsToSet, ProjectInfo);

        if (changed.Any(field => field.MarksNewFieldUsage))
        {
            ProjectMetadataChanged = true;
        }

        return changed;
    }

    /// <summary>
    /// То же, но через заявку: персонаж берётся из неё, а выбор стратегии в
    /// <see cref="CharacterFields.FieldSaveHelper"/> зависит от того, утверждена ли заявка.
    /// </summary>
    /// <param name="claim">Заявка, через которую сохраняются поля.</param>
    /// <param name="fieldsToSet">
    /// Дельта, а не полный слой. Пустой слой не означает «ничего не делать»: пересохранение пустым
    /// слоем нужно ради побочных эффектов — генерации значений по умолчанию, переноса значений
    /// заявка→персонаж и пересчёта спецгрупп.
    /// </param>
    /// <returns>Изменившиеся поля.</returns>
    protected IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveFieldsCore(
        Claim claim,
        FieldLayerContainer fieldsToSet)
    {
        var changed = FieldSaveHelper.SaveCharacterFields(CurrentUser.UserId, claim, fieldsToSet, ProjectInfo);

        if (changed.Any(field => field.MarksNewFieldUsage))
        {
            ProjectMetadataChanged = true;
        }

        return changed;
    }
}

/// <summary>
/// Контекст изменения существующего персонажа. Негенерик — чтобы приватные хелперы сервисов
/// принимали его без параметра типа (как <c>ProjectMutationContext</c>, ADR009).
/// </summary>
/// <param name="Character">Трекаемая EF-сущность персонажа; её и нужно мутировать.</param>
/// <param name="CharacterInfo">Доменный снимок персонажа строго ДО изменения.</param>
/// <param name="Scope">Что операции разрешено делать с БД: добавить, удалить, догрузить соседей.</param>
internal abstract record CharacterMutationContext(
    Character Character,
    CharacterInfo CharacterInfo,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    IAggregateMutationScope Scope,
    FieldSaveHelper FieldSaveHelper)
    : CharacterOperationContext(ProjectInfo, Now, CurrentUser, FieldSaveHelper)
{
    /// <summary>Добавляет сущность в тот же <c>DbContext</c>, через который идёт сохранение.</summary>
    public void AddEntity(object entity) => Scope.Add(entity);

    /// <summary>Окончательно удаляет сущность из того же <c>DbContext</c>.</summary>
    public void RemoveEntity(object entity) => Scope.Remove(entity);

    /// <inheritdoc cref="CharacterOperationContext.SaveFieldsCore"/>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveFields(FieldLayerContainer fieldsToSet)
        => SaveFieldsCore(Character, fieldsToSet);
}

/// <summary>
/// Контекст изменения персонажа с типизированными аргументами операции (<see cref="Request"/>).
/// </summary>
internal sealed record CharacterMutationContext<TArgs>(
    Character Character,
    CharacterInfo CharacterInfo,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    IAggregateMutationScope Scope,
    FieldSaveHelper FieldSaveHelper,
    TArgs Request)
    : CharacterMutationContext(Character, CharacterInfo, ProjectInfo, Now, CurrentUser, Scope, FieldSaveHelper);

/// <summary>
/// Контекст создания персонажа: самого персонажа ещё нет, фабрика строит сущность с нуля.
/// </summary>
/// <param name="Project">Трекаемая EF-сущность проекта, в который добавляется персонаж.</param>
internal abstract record CharacterCreationContext(
    Project Project,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    FieldSaveHelper FieldSaveHelper)
    : CharacterOperationContext(ProjectInfo, Now, CurrentUser, FieldSaveHelper)
{
    /// <inheritdoc cref="CharacterOperationContext.SaveFieldsCore"/>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveFields(
        Character character,
        FieldLayerContainer fieldsToSet)
        => SaveFieldsCore(character, fieldsToSet);
}

/// <summary>
/// Контекст создания персонажа с типизированными аргументами операции (<see cref="Request"/>).
/// </summary>
internal sealed record CharacterCreationContext<TArgs>(
    Project Project,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    FieldSaveHelper FieldSaveHelper,
    TArgs Request)
    : CharacterCreationContext(Project, ProjectInfo, Now, CurrentUser, FieldSaveHelper);
