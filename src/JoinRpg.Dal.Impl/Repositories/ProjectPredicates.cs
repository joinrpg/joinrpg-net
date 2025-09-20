using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using LinqKit;

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

    public static Expression<Func<Project, bool>> MasterAccess(UserIdentification userInfoId)
        => project => project.ProjectAcls.Any(projectAcl => projectAcl.UserId == userInfoId.Value);

    public static Expression<Func<Project, bool>> HasActiveClaim(UserIdentification userInfoId)
    {
        var claimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        return project => project.Claims.Where(c => c.PlayerUserId == userInfoId.Value).Any(claimPredicate.Compile());
    }

    public static Expression<Func<Project, bool>> BySpecification(UserIdentification? userInfoId, ProjectListSpecification projectListSpecification)
    {
        var predicate = PredicateBuilder.New<Project>();
        if (!projectListSpecification.LoadArchived)
        {
            predicate = predicate.And(p => p.Active);
        }

        predicate = projectListSpecification.Criteria switch
        {
            ProjectListCriteria.All => predicate.And(p => true),
            ProjectListCriteria.MasterAccess when userInfoId is not null => predicate.And(MasterAccess(userInfoId)),
            ProjectListCriteria.MasterOrActiveClaim when userInfoId is not null => predicate.And(PredicateBuilder.New<Project>().Or(HasActiveClaim(userInfoId)).Or(MasterAccess(userInfoId))),
            ProjectListCriteria.ForCloning when userInfoId is not null => predicate.And(ForCloning(userInfoId)),
            ProjectListCriteria.HasSchedule => predicate.And(project => project.Details.ScheduleEnabled),
            ProjectListCriteria.NoKogdaIgra => predicate.And(project => project.KogdaIgraGames.Count == 0),
            ProjectListCriteria.MasterGrantAccess when userInfoId is not null => predicate.And(project => project.ProjectAcls.Any(projectAcl => projectAcl.UserId == userInfoId.Value && projectAcl.CanGrantRights)),
            _ => throw new NotImplementedException(),
        };
        return predicate;
    }

    private static Expression<Func<Project, bool>> ForCloning(UserIdentification userInfoId)
    {
        return project =>
             project.Details.ProjectCloneSettings == ProjectCloneSettings.CanBeClonedByAnyone
             || (project.Details.ProjectCloneSettings == ProjectCloneSettings.CanBeClonedByMaster && MasterAccess(userInfoId).Compile()(project));
    }
}
