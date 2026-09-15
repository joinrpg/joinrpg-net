using System.ComponentModel;
using JoinRpg.DomainTypes;
using JoinRpg.Interfaces;
using JoinRpg.WebPortal.Managers.Projects;
using JoinRpg.XGameApi.Contract;
using ModelContextProtocol.Server;

namespace JoinRpg.Mcp;

[McpServerToolType]
public sealed class ProjectMcpTools(
    IProjectApiViewService projectApiViewService,
    ICurrentUserAccessor currentUserAccessor,
    McpAuthContext authContext)
{
    [McpServerTool(Name = "list_projects"), Description(
        "Проекты, где пользователь — мастер, из числа выбранных на экране согласия OAuth. " +
        "Всегда вызывай первым, чтобы получить id проектов для остальных инструментов.")]
    public async Task<IReadOnlyCollection<ProjectHeader>> ListProjects()
    {
        var projects = await projectApiViewService.ListMasterProjects(currentUserAccessor.UserIdentification);
        return [.. projects.Where(p => authContext.IsProjectGranted(p.ProjectId))];
    }

    [McpServerTool(Name = "get_project_overview"), Description(
        "Дерево групп персонажей (включая спецгруппы вариантов полей) и все поля/варианты проекта " +
        "одним вызовом. Вызывай первым для проекта, прежде чем list_characters/search_characters/get_characters — " +
        "значения полей и id групп бери отсюда, не выдумывай.")]
    public async Task<ProjectOverview> GetProjectOverview(
        [Description("Id проекта")] int projectId)
    {
        authContext.EnsureProjectGranted(projectId);
        return await projectApiViewService.GetOverview(new ProjectIdentification(projectId));
    }
}
