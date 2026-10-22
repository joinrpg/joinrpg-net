using JoinRpg.DomainTypes.ProjectMetadata.Badges;

namespace JoinRpg.Data.Interfaces.ProjectMetadata;

/// <summary>
/// Шаблоны бейджей проекта. Нужны редко (печать, редактор, регистрация), поэтому в <c>ProjectInfo</c>
/// не входят и грузятся отдельно.
/// </summary>
public interface IProjectBadgeTemplateRepository
{
    /// <summary>Все шаблоны проекта, включая удалённые (<see cref="ProjectBadgeTemplate.IsActive"/>)</summary>
    Task<IReadOnlyCollection<ProjectBadgeTemplate>> GetForProjectAsync(ProjectIdentification projectId);
    Task<ProjectBadgeTemplate?> GetByIdAsync(ProjectBadgeTemplateIdentification id);
}
