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
    /// Пересобирает <see cref="ProjectInfo"/> из текущего состояния <see cref="Project"/>
    /// и возвращает его.
    /// </summary>
    ProjectInfo Refresh();
}
