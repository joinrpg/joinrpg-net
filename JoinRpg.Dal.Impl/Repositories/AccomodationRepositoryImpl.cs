using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  public class AccomodationRepositoryImpl:RepositoryImplBase, IAccomodationRepository
  {
    public AccomodationRepositoryImpl(MyDbContext ctx) :base(ctx)
    {
    }

    public IQueryable<ProjectAccomodationType> GetAccomodationForProject(Project project)
    {
      return Ctx.Set<ProjectAccomodationType>().Where(a=>a.ProjectId == project.ProjectId);
    }

 

  }
}
