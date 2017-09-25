using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IAccomodationRepository
  {
     IQueryable<ProjectAccomodationType> GetAccomodationForProject(Project project);
  }
}
