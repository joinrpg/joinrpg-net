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
    public class AccommodationServiceImpl : DbServiceImplBase, IAccommodationService
    {
        public async Task<ProjectAccommodationType> RegisterNewAccommodationTypeAsync(ProjectAccommodationType newAccommodation)
        {
            if (newAccommodation.ProjectId == 0) throw new ActivationException("Inconsistent state. ProjectId can't be 0");
            ProjectAccommodationType result = null;
            if (newAccommodation.Id != 0)
            {
                result = await UnitOfWork.GetDbSet<ProjectAccommodationType>().FindAsync(newAccommodation.Id).ConfigureAwait(false);
                if (result?.ProjectId != newAccommodation.ProjectId)
                {
                    return null;
                }
                result.Name = newAccommodation.Name;
                result.Cost = newAccommodation.Cost;
                result.Capacity = newAccommodation.Capacity;
                result.Description = newAccommodation.Description;
                result.IsAutoFilledAccommodation = newAccommodation.IsAutoFilledAccommodation;
                result.IsInfinite = newAccommodation.IsInfinite;
                result.IsPlayerSelectable = newAccommodation.IsPlayerSelectable;

            }
            else
            {
                result = UnitOfWork.GetDbSet<ProjectAccommodationType>().Add(newAccommodation);
            }
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
            return result;
        }


        public async Task<ProjectAccommodation> RegisterNewProjectAccommodationAsync(ProjectAccommodation newProjectAccommodation)
        {
            if (newProjectAccommodation.ProjectId == 0) throw new ActivationException("Inconsistent state. ProjectId can't be 0");
            ProjectAccommodation result = null;
            if (newProjectAccommodation.Id != 0)
            {
                result = await UnitOfWork.GetDbSet<ProjectAccommodation>().FindAsync(newProjectAccommodation.Id).ConfigureAwait(false);
                if (result?.ProjectId != newProjectAccommodation.ProjectId || result?.AccommodationTypeId != newProjectAccommodation.AccommodationTypeId)
                {
                    throw new ProjectAccomodationNotFound(newProjectAccommodation.ProjectId,
                        newProjectAccommodation.AccommodationTypeId,
                        newProjectAccommodation.Id);
                }
                result.Name = newProjectAccommodation.Name;
            }
            else
            {
                result = UnitOfWork.GetDbSet<ProjectAccommodation>().Add(newProjectAccommodation);
            }
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
            return result;
        }

        public async Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int projectId)
        {
            return await AccomodationRepository.GetAccommodationForProject(projectId).ConfigureAwait(false);
        }

        public async Task<ProjectAccommodationType> GetAccommodationByIdAsync(int accId)
        {
            return await UnitOfWork.GetDbSet<ProjectAccommodationType>().Include(x=>x.ProjectAccommodations)
              .FirstOrDefaultAsync(x => x.Id == accId).ConfigureAwait(false);
        }
        public async Task<ProjectAccommodation> GetProjectAccommodationByIdAsync(int accId)
        {
            return await UnitOfWork.GetDbSet<ProjectAccommodation>()
                .FirstOrDefaultAsync(x => x.Id == accId).ConfigureAwait(false);
        }

        public async Task RemoveAccommodationType(int accomodationTypeId)
        {
            var entity = UnitOfWork.GetDbSet<ProjectAccommodationType>().Find(accomodationTypeId);

            if (entity == null)
            {
                throw new JoinRpgEntityNotFoundException(accomodationTypeId, "ProjectAccommodationType");
            }
            UnitOfWork.GetDbSet<ProjectAccommodationType>().Remove(entity);
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        }

        public async Task RemoveProjectAccommodation(int projectAccomodationId)
        {
            var entity = UnitOfWork.GetDbSet<ProjectAccommodation>().Find(projectAccomodationId);

            if (entity == null)
            {
                throw new JoinRpgEntityNotFoundException(projectAccomodationId, "ProjectAccommodation");
            }
            UnitOfWork.GetDbSet<ProjectAccommodation>().Remove(entity);
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        }

        public AccommodationServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}
