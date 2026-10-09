using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Репозиторий для изменения метаданных проекта. Гарантирует согласованность
/// EF-сущности <see cref="Project"/> и доменного снимка <see cref="ProjectInfo"/>.
/// </summary>
public interface IProjectMetadataWriteRepository
{
    /// <summary>
    /// Грузит трекаемый <see cref="Project"/> со всеми связями и согласованный с ним
    /// <see cref="ProjectInfo"/>.
    /// </summary>
    Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId);
}

/// <summary>
/// Пара (трекаемый <see cref="Project"/>, согласованный с ним <see cref="ProjectInfo"/>).
/// После мутации <see cref="Project"/> вызвать <see cref="Refresh"/>, чтобы получить актуальный
/// <see cref="ProjectInfo"/> без обращения в БД.
/// </summary>
public interface IProjectMetadataUpdateHandle : IProjectMetadataMutationScope
{
    /// <summary>Трекаемая EF-сущность проекта; именно её нужно мутировать.</summary>
    Project Project { get; }

    /// <summary>
    /// Снимок метаданных. До вызова <see cref="Refresh"/> — состояние ДО изменения.
    /// </summary>
    ProjectInfo ProjectInfo { get; }

    /// <summary>
    /// Перечитывает <see cref="Project"/> из БД (тем же <c>DbContext</c>, значит — в той же
    /// транзакции) и пересобирает из него <see cref="ProjectInfo"/>. Перечитывание, а не пересборка
    /// по уже загруженному в памяти графу, нужно, чтобы сущности, добавленные в рамках мутации
    /// (например новый <c>ProjectAcl</c>), получили все navigation-свойства: EF6 lazy loading не
    /// работает для сущностей, созданных через <c>new</c>, а не через прокси-фабрику контекста.
    /// </summary>
    Task<ProjectInfo> Refresh();
}

/// <summary>
/// Что операции над метаданными проекта разрешено делать с БД внутри мутации: удалить под-сущность
/// и догрузить связи пачкой. Всё — через тот же <c>DbContext</c>, что и последующий <c>SaveChanges</c>.
/// </summary>
/// <remarks>
/// Единый интерфейс вместо делегата на каждую догрузку — симметрично
/// <see cref="Characters.IAggregateMutationScope"/> на стороне персонажа: делегаты пришлось бы
/// протаскивать параметрами через оба контекста мутации, а их число растёт с каждой новой
/// догрузкой. Доступ при этом не расширяется: произвольного репозитория здесь нет, только
/// именованные операции.
/// </remarks>
public interface IProjectMetadataMutationScope
{
    /// <summary>
    /// Окончательно удаляет под-сущность проекта из того же <c>DbContext</c>, через который потом
    /// вызывается <c>SaveChanges</c>. Используется для permanent-delete (см. SmartDelete).
    /// </summary>
    void Remove(object entity);

    /// <summary>
    /// Одним запросом загружает вводные, нацеленные на группы (<see cref="CharacterGroup.DirectlyRelatedPlotElements"/>).
    /// Нужно перед удалением групп в цикле: <see cref="CharacterGroup.CanBePermanentlyDeleted"/> смотрит
    /// на эту коллекцию, и без предзагрузки каждая группа дала бы свою ленивую загрузку (#5269).
    /// </summary>
    void LoadPlotTargetsOfGroups(IReadOnlyCollection<CharacterGroup> groups);
}
