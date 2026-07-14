using System.Diagnostics;

namespace JoinRpg.Services.Impl.Projects;

public static class ProjectPropsServiceActivity
{
    public const string ActivitySourceName = nameof(ProjectPropsService);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
