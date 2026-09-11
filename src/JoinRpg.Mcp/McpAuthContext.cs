using Microsoft.AspNetCore.Http;
using ModelContextProtocol;

namespace JoinRpg.Mcp;

/// <summary>
/// Проекты, выбранные пользователем на экране согласия OAuth (ADR012 §4), читаются из токена
/// по claim <c>projects</c>. Managers-слой сам проверяет <c>HasMasterAccess</c> — этот класс
/// добавляет единственную MCP-специфичную проверку поверх: доступ только к тем проектам,
/// которые явно выбраны в согласии, даже если пользователь мастер и в других.
/// </summary>
public class McpAuthContext(IHttpContextAccessor httpContextAccessor)
{
    // Должно совпадать с OAuthConsent.ProjectsClaimType в JoinRpg.IdPortal.OAuthServer —
    // общего кода между Portal и IdPortal здесь нет, это контракт токена.
    private const string ProjectsClaimType = "projects";

    private IReadOnlySet<int> GrantedProjectIds => field ??= Parse(
        httpContextAccessor.HttpContext?.User.FindFirst(ProjectsClaimType)?.Value);

    public bool IsProjectGranted(int projectId) => GrantedProjectIds.Contains(projectId);

    public void EnsureProjectGranted(int projectId)
    {
        if (!IsProjectGranted(projectId))
        {
            throw new McpException($"Доступ к проекту {projectId} не предоставлен в согласии OAuth — переподключитесь с этим проектом в списке.");
        }
    }

    private static HashSet<int> Parse(string? raw) =>
        string.IsNullOrEmpty(raw)
            ? new HashSet<int>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToHashSet();
}
