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


        public async Task<IEnumerable<ProjectAccommodation>> RegisterNewProjectAccommodationAsync(ProjectAccommodation newProjectAccommodation)
        {
            if (newProjectAccommodation.ProjectId == 0)
                throw new ActivationException("Inconsistent state. ProjectId can't be 0");
            ProjectAccommodation result = null;
            IEnumerable<ProjectAccommodation> results = null;

            //TODO: Implement rooms names checking
            //TODO: Remove result variable

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
                // Creates new room using name and parameters from given room info
                ProjectAccommodation CreateRoom(ProjectAccommodation input, string name)
                    => new ProjectAccommodation
                    {
                        Name = name,
                        AccommodationTypeId = input.AccommodationTypeId,
                        ProjectId = input.ProjectId,
                        Id = 0
                    };

                // Iterates through rooms list and creates object for each room from a list
                IEnumerable<ProjectAccommodation> CreateRooms(ProjectAccommodation input)
                {
                    foreach (string roomCandidate in input.Name.Split(','))
                    {
                        int rangePos = roomCandidate.IndexOf('-');
                        if (rangePos > -1)
                        {
                            if (int.TryParse(roomCandidate.Substring(0, rangePos).Trim(), out int roomsRangeStart)
                                && int.TryParse(roomCandidate.Substring(rangePos + 1).Trim(), out int roomsRangeEnd)
                                && roomsRangeStart < roomsRangeEnd)
                            {
                                while (roomsRangeStart <= roomsRangeEnd)
                                {
                                    yield return CreateRoom(input, roomsRangeStart.ToString());
                                    roomsRangeStart++;
                                }
                                // Range was defined correctly, we can continue to next item
                                continue;
                            }
                        }

                        yield return CreateRoom(input, roomCandidate.Trim());
                    }
                }

                // Inserting range of rooms instead one
                results = UnitOfWork.GetDbSet<ProjectAccommodation>().AddRange(CreateRooms(newProjectAccommodation));
                //result = UnitOfWork.GetDbSet<ProjectAccommodation>().Add(newProjectAccommodation);
            }
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

            if (result != null)
                return new ProjectAccommodation[] { result };
            return results;
            //return result;
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

        public Task OccupyRoom(OccupyRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task UnOccupyRoom(UnOccupyRequest request)
        {
            throw new System.NotImplementedException();
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
