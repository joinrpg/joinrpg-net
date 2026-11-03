using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

/// <summary>
/// Read-only клиент игроцкого отображения сетки ролей.
/// Не путать с мастерским <c>IProjectRolesListClient</c> (управление) в ProjectMasterTools.
/// </summary>
public interface IProjectRoleGridClient
{
    Task<ProjectRoleGridViewResult> GetRoleGrid(ProjectRolesListIdentification id);

    /// <summary>
    /// Сетка, вшитая в вводную (ADR029). Доступ — по правилам показа вводной: мастеру всегда,
    /// остальным — только для опубликованной вводной при публичной сетке, публикации вводных
    /// проекта или наличии approved/checked-in заявки на персонажа из её таргетов.
    /// </summary>
    Task<ProjectRoleGridViewResult> GetRoleGridForPlot(PlotElementIdentification plotElementId);

    /// <summary>
    /// «Классическая» сетка ролей (страница GameGroups/Index) — строится на лету, без сохранённой
    /// настройки. <paramref name="groupId"/> — явно выбранный корень сетки, или null, если корень
    /// не указан в URL: тогда сетка строится от корня проекта, не используя спецгруппы.
    /// <paramref name="hotOnly"/> — строить сетку горячих ролей (страница GameGroups/Hot): плоский
    /// список без групп, только персонажи с <c>IsHot == true</c>, вместо обычной классической сетки.
    /// </summary>
    Task<ProjectRoleGridViewResult> GetClassicRoleGrid(ProjectIdentification projectId, CharacterGroupIdentification? groupId, bool hotOnly = false);
}
