using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using Microsoft.Practices.ServiceLocation;

namespace JoinRpg.Services.Impl
{
    public class AccomodationServiceImpl : DbServiceImplBase, IAccomodationService
    {
        public async Task<ProjectAccomodationType> RegisterNewAccomodationTypeAsync(ProjectAccomodationType newAccomodation)
        {
            if (newAccomodation.ProjectId == 0) throw new ActivationException("Inconsistent state. ProjectId can't be 0");
            ProjectAccomodationType result = null;
            if (newAccomodation.Id != 0)
            {
                result = await UnitOfWork.GetDbSet<ProjectAccomodationType>().FindAsync(newAccomodation.Id);
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

        public async Task<IReadOnlyCollection<ProjectAccomodationType>> GetAccomodationForProject(int projectId)
        {
            return await AccomodationRepository.GetAccomodationForProject(projectId);
        }

        public async Task<ProjectAccomodationType> GetAccomodationByIdAsync(int accId)
        {
            return await UnitOfWork.GetDbSet<ProjectAccomodationType>()
              .FirstOrDefaultAsync(x => x.Id == accId);
        }
        public async Task RemoveAccomodationType(int accomodationTypeId)
        {
            var entity = UnitOfWork.GetDbSet<ProjectAccomodationType>().Find(accomodationTypeId);

            if (entity == null)
            {
                throw new JoinRpgEntityNotFoundException(accomodationTypeId, "ProjectAccomodationType");
            }
            UnitOfWork.GetDbSet<ProjectAccomodationType>().Remove(entity);
            await UnitOfWork.SaveChangesAsync();

        }

        public AccomodationServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}
