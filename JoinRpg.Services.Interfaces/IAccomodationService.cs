using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IAccomodationService
  {
    Task<ProjectAccomodationType> RegisterNewAccomodationTypeAsync(ProjectAccomodationType newAccomodation);
    Task RemoveAccomodationType(int accomodationTypeIndex);
    IQueryable<ProjectAccomodationType> GetAccomodationForProject(Project project);
    Task<ProjectAccomodationType> GetAccomodationByIdAsync(int accId);
  }
}
