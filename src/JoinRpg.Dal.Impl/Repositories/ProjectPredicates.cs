using System.Linq.Expressions;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class ProjectPredicates
{
    public static Expression<Func<Project, bool>> Status(ProjectLifecycleStatus status)
    {
        return status switch
        {
            ProjectLifecycleStatus.ActiveClaimsClosed => project => project.Active && !project.IsAcceptingClaims,
            ProjectLifecycleStatus.ActiveClaimsOpen => project => project.Active && project.IsAcceptingClaims,
            ProjectLifecycleStatus.Archived => project => !project.Active,
            _ => throw new NotImplementedException(),
        };
    }

    public static Expression<Func<Project, bool>> Active() => project => project.Active;

    public static Expression<Func<Project, bool>> MyProjectPredicate(UserIdentification userInfoId)
        => project => project.ProjectAcls.Any(projectAcl => projectAcl.UserId == userInfoId.Value);
}
