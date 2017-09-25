using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  public class AccomodationServiceImpl : DbServiceImplBase, IAccomodationService
  {
    public async Task<ProjectAccomodationType> RegisterNewAccomodationTypeAsync(ProjectAccomodationType newAccomodation)
    {
      if (newAccomodation.ProjectId == 0) return null;
      ProjectAccomodationType result = null;
      if (newAccomodation.Id != 0)
      {
        result = UnitOfWork.GetDbSet<ProjectAccomodationType>().Find(newAccomodation.Id);
        if (result?.ProjectId != newAccomodation.ProjectId)
        {
          return null;
        }
        result.Name = newAccomodation.Name;
        result.Cost = newAccomodation.Cost;
      }
      else
      {
        result = UnitOfWork.GetDbSet<ProjectAccomodationType>().Add(newAccomodation);
      }
      await UnitOfWork.SaveChangesAsync();
      return result;
    }

    public IQueryable<ProjectAccomodationType> GetAccomodationForProject(Project project)
    {
      return AccomodationRepository.GetAccomodationForProject(project);
    }

    public async Task<ProjectAccomodationType> GetAccomodationByIdAsync(int accId)
    {
      return await UnitOfWork.GetDbSet<ProjectAccomodationType>()
        .FirstOrDefaultAsync(x => x.Id == accId);
    }
    public async Task RemoveAccomodationType(int accomodationTypeIndex)
    {
      var entity = UnitOfWork.GetDbSet<ProjectAccomodationType>().Find(accomodationTypeIndex);
      if (entity != null)
      {
        UnitOfWork.GetDbSet<ProjectAccomodationType>().Remove(entity);
        await UnitOfWork.SaveChangesAsync();
      }
    }

    public AccomodationServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
