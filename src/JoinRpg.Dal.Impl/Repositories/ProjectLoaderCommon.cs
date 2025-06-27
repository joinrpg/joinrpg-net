using System.Data.Entity;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories;
internal static class ProjectLoaderCommon
{
    public static async Task<Project?> GetProjectWithFieldsAsync(MyDbContext ctx, int project, bool skipCache)
    {
        var query = skipCache ? ctx.ProjectsSet.AsNoTracking() : ctx.ProjectsSet;
        return await query
         .Include(p => p.Details)
         .Include(p => p.ProjectAcls.Select(a => a.User))
         .Include(p => p.ProjectFields.Select(f => f.DropdownValues))
         .Include(p => p.PaymentTypes)
         .SingleOrDefaultAsync(p => p.ProjectId == project);
    }
}
