using JoinRpg.DomainTypes.ProjectMetadata.Badges;

namespace JoinRpg.Web.ProjectMasterTools.Badges;

/// <summary>Ссылки на страницы шаблонов бейджей</summary>
public interface IBadgeTemplateUriLocator
{
    /// <summary>Список шаблонов проекта</summary>
    Uri GetListUri(ProjectIdentification projectId);

    /// <summary>Создание нового шаблона</summary>
    Uri GetCreateUri(ProjectIdentification projectId);

    /// <summary>Правка шаблона</summary>
    Uri GetEditUri(ProjectBadgeTemplateIdentification id);
}
