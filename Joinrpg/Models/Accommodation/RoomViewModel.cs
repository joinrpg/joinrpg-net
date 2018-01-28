using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using WebGrease.Css.Extensions;

namespace JoinRpg.Web.Models.Accommodation
{

    public class RoomTypeRoomsListViewModel
    {

        public RoomTypeRoomsListViewModel(RoomTypeViewModel roomType)
        {
            ProjectId = roomType.ProjectId;
            RoomTypeId = roomType.Id;
            Rooms = roomType.Rooms ?? null;
            RoomCapacity = roomType.Capacity;
        }

        public ICollection<RoomViewModel> Rooms { get; set; }

        public int ProjectId { get; set; }

        public int RoomTypeId { get; set; }

        public int RoomCapacity { get; set; }

    }

    public class RoomInhabitantViewModel
    {

        public RoomInhabitantViewModel(RoomViewModel room, ClaimViewModel claim)
        {
            Claim = claim;
            ProjectId = room.ProjectId;
            RoomId = room.Id;
        }

        public int ProjectId { get; set; }

        public int RoomId { get; set; }

        public ClaimViewModel Claim { get; set; }

    }

    public class RoomViewModel
    {
        public int Id { get; set; }
        public int RoomTypeId { get; set; }
        public int ProjectId { get; set; }
        [Required]
        [DisplayName("Название (номер)")]
        public string Name { get; set; }
        public virtual ICollection<ClaimViewModel> Inhabitants { get; set; }

        public int Capacity { get; set; }

        public int Occupancy
            => Inhabitants?.Count ?? 0;


        public RoomViewModel([NotNull]ProjectAccommodation entity)
        {
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }
            Id = entity.Id;
            Name = entity.Name;
            ProjectId = entity.ProjectId;
            RoomTypeId = entity.AccommodationTypeId;
            Capacity = entity.ProjectAccommodationType?.Capacity ?? 0;            
        }

        public RoomViewModel(RoomTypeViewModel roomType)
        {
            Id = 0;
            ProjectId = roomType.ProjectId;
            RoomTypeId = roomType.Id;
            Capacity = roomType.Capacity;
        }

        public RoomViewModel()
        { }

        public ProjectAccommodation GetProjectAccommodationMock()
        {
            return new ProjectAccommodation()
            {
                Id = Id,
                ProjectId = ProjectId,
                Name = Name,
                AccommodationTypeId = RoomTypeId
            };
        }

        internal static ICollection<RoomViewModel> NewListCollection([NotNull] ICollection<ProjectAccommodation> projectAccomodations)
        {
            //TODO: add claim calculation logic 
            return projectAccomodations.Select(acc => new RoomViewModel(acc))
                .ToSafeReadOnlyCollection();
            
        }
     
    }
    
}
