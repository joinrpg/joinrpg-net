using JoinRpg.DataModel.Projects;
using JoinRpg.PrimitiveTypes.Claims;
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

    public static Expression<Func<Project, bool>> BySpecification(ProjectListSpecification projectListSpecification)
    {
        var predicate = PredicateBuilder.New<Project>();
        if (!projectListSpecification.LoadArchived)
        {
            predicate = predicate.And(p => p.Active);
        }

        Expression<Func<KogdaIgraGame, bool>> kogdaIgraStaleExpression60 = KogdaIgraIsStaleFor(TimeSpan.FromDays(60));

        predicate = projectListSpecification switch
        {
            { Criteria: ProjectListCriteria.All } => predicate.And(p => true),
            { Criteria: ProjectListCriteria.Public } => predicate.And(p => p.Details.IsPublicProject)
                .And(project => projectListSpecification.LoadArchived || project.Details.DisableKogdaIgraMapping || project.KogdaIgraGames.Count() == 0 || project.KogdaIgraGames.Any(k => k.End > DateTime.Now)),
            PersonalizedProjectListSpecification { Criteria: ProjectListCriteria.MasterAccess, UserId: var userId } => predicate.And(MasterAccess(userId)),
            PersonalizedProjectListSpecification { Criteria: ProjectListCriteria.MasterOrActiveClaim, UserId: var userId }
                => predicate.And(PredicateBuilder.New<Project>().Or(HasActiveClaim(userId)).Or(MasterAccess(userId))),
            PersonalizedProjectListSpecification { Criteria: ProjectListCriteria.ForCloning, UserId: var userId }
                => predicate.And(ForCloning(userId)),
            { Criteria: ProjectListCriteria.HasSchedule } => predicate.And(project => project.Details.ScheduleEnabled),
            PersonalizedProjectListSpecification { Criteria: ProjectListCriteria.MasterGrantAccess, UserId: var userId } => predicate.And(project => project.ProjectAcls.Any(projectAcl => projectAcl.UserId == userId.Value && projectAcl.CanGrantRights)),
            _ => throw new NotImplementedException(),
        };
        return predicate;
    }

    internal static Expression<Func<KogdaIgraGame, bool>> KogdaIgraIsStaleFor(TimeSpan timeSpan)
    {
        var date = DateTime.Now.Subtract(timeSpan);
        return e => e.End > date;
    }

    private static Expression<Func<Project, bool>> ForCloning(UserIdentification userInfoId)
    {
        return project =>
             project.Details.ProjectCloneSettings == ProjectCloneSettings.CanBeClonedByAnyone
             || (project.Details.ProjectCloneSettings == ProjectCloneSettings.CanBeClonedByMaster && MasterAccess(userInfoId).Compile()(project));
    }

    internal static Expression<Func<Project, bool>> Public() => p => p.Details.IsPublicProject;
}
