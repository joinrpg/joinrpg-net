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
            CanManageRooms = roomType.CanManageRooms;
            CanAssignRooms = roomType.CanAssignRooms;
        }

        public ICollection<RoomViewModel> Rooms { get; set; }

        public int ProjectId { get; set; }

        public int RoomTypeId { get; set; }

        public int RoomCapacity { get; set; }

        public bool CanManageRooms { get; }

        public bool CanAssignRooms { get; }

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


        public bool CanManageRooms { get; }

        public bool CanAssignRoom { get; set; }

        public RoomViewModel([NotNull]ProjectAccommodation entity, bool canManageRooms, bool canAssignRoom)
        {
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }

            CanManageRooms = canManageRooms;
            CanAssignRoom = canAssignRoom;
            Id = entity.Id;
            Name = entity.Name;
            ProjectId = entity.ProjectId;
            RoomTypeId = entity.AccommodationTypeId;
            Capacity = entity.ProjectAccommodationType?.Capacity ?? 0;            
        }

        public RoomViewModel()
        {
        }

        internal static ICollection<RoomViewModel> NewListCollection([NotNull]
            ICollection<ProjectAccommodation> projectAccomodations,
            bool canManageRooms,
            bool canAssignRoom)
        {
            //TODO: add claim calculation logic 
            return projectAccomodations.Select(acc => new RoomViewModel(acc, canManageRooms, canAssignRoom))
                .ToSafeReadOnlyCollection();
        }
     
    }
    
}
