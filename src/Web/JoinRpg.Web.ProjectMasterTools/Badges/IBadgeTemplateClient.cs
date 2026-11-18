using JoinRpg.DomainTypes.ProjectMetadata.Badges;

namespace JoinRpg.Web.ProjectMasterTools.Badges;

/// <summary>Шаблоны бейджей проекта — для редактора (серверная реализация и WebApi-клиент острова)</summary>
public interface IBadgeTemplateClient
{
    Task<BadgeTemplateListViewModel> GetList(ProjectIdentification projectId);

    Task<EditBadgeTemplateViewModel> GetById(ProjectBadgeTemplateIdentification id);

    Task<ProjectBadgeTemplateIdentification> Create(ProjectIdentification projectId, EditBadgeTemplateViewModel model);

    Task Update(ProjectBadgeTemplateIdentification id, EditBadgeTemplateViewModel model);

    Task Remove(ProjectBadgeTemplateIdentification id);

    Task Restore(ProjectBadgeTemplateIdentification id);
}
