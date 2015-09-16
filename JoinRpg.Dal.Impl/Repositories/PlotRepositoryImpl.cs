using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  public class PlotRepositoryImpl : RepositoryImplBase, IPlotRepository
  {
    public Task<PlotFolder> GetPlotFolderAsync(int projectId, int plotFolderId)
    {
      return
        Ctx.Set<PlotFolder>()
          .Include(pf => pf.Project)
          .Include(pf => pf.Project.ProjectAcls)
          .Include(pf => pf.Elements)
          .Include(pf => pf.Project.Characters)
          .Include(pf => pf.Project.CharacterGroups)
          .Include(pf => pf.Project.Claims)
          .SingleOrDefaultAsync(pf => pf.PlotFolderId == plotFolderId && pf.ProjectId == projectId);
    }

    public Task<List<PlotFolder>> GetPlots(int project)
      => Ctx.Set<PlotFolder>().Include(pf => pf.Elements).Where(pf => pf.ProjectId == project).ToListAsync();

    public PlotRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }
  }
}
