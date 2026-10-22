using JoinRpg.DomainTypes.ProjectMetadata.Badges;

namespace JoinRpg.Services.Interfaces.ProjectMetadata;

/// <summary>
/// Изменение шаблонов бейджей проекта. Поля проекта, на которые ссылается шаблон, помечаются
/// использованными — после этого их удаление мягкое.
/// </summary>
public interface IProjectBadgeTemplateService
{
    Task<ProjectBadgeTemplateIdentification> CreateAsync(ProjectIdentification projectId, string name, BadgeLayout layout);

    Task UpdateAsync(ProjectBadgeTemplateIdentification id, string name, BadgeLayout layout);

    /// <summary>Выключает шаблон. Из БД он не удаляется — см. <see cref="RestoreAsync"/></summary>
    Task RemoveAsync(ProjectBadgeTemplateIdentification id);

    Task RestoreAsync(ProjectBadgeTemplateIdentification id);
}
