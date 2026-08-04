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
public interface IProjectMetadataUpdateHandle
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

    /// <summary>
    /// Окончательно удаляет под-сущность проекта из того же <c>DbContext</c>, через который потом
    /// вызывается <c>SaveChanges</c>. Используется для permanent-delete (см. SmartDelete).
    /// </summary>
    void Remove(object entity);
}
