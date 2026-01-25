using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class ProjectLoaderCommon
{
    // TODO не грузить тут лишнего, в частности в Details
    public static async Task<Project?> GetProjectWithFieldsAsync(MyDbContext ctx, int project, bool skipCache)
    {
        var query = skipCache ? ctx.ProjectsSet.AsNoTracking() : ctx.ProjectsSet;
        return await query
         .Include(p => p.Details)
         .Include(p => p.ProjectAcls.Select(a => a.User))
         .Include(p => p.ProjectFields.Select(f => f.DropdownValues))
         .Include(p => p.PaymentTypes.Select(p => p.User))
         .Include(p => p.KogdaIgraGames)
         .SingleOrDefaultAsync(p => p.ProjectId == project);
    }

    public static ProjectLifecycleStatus CreateStatus(bool active, bool isAcceptingClaims)
    {
        return (active, isAcceptingClaims) switch
        {
            (true, false) => ProjectLifecycleStatus.ActiveClaimsClosed,
            (true, true) => ProjectLifecycleStatus.ActiveClaimsOpen,
            (false, false) => ProjectLifecycleStatus.Archived,
            (false, true) => throw new InvalidOperationException()
        };
    }
}
