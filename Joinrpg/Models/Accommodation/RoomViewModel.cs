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

    /// <summary>
    /// View model for single room inhabitant
    /// </summary>
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

    /// <summary>
    /// View model for single room
    /// </summary>
    public class RoomViewModel
    {
        public int Id { get; set; }

        public int RoomTypeId { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [DisplayName("Название (номер)")]
        public string Name { get; set; }

        //public virtual ICollection<ClaimViewModel> Inhabitants { get; set; }

        public IReadOnlyList<AccRequestViewModel> Requests { get; set; }

        public int Capacity { get; set; }

        public int Occupancy { get; private set; }

        public bool CanManageRooms { get; }

        public bool CanAssignRooms { get; set; }

        public RoomViewModel()
        {
        }

        public RoomViewModel([NotNull]ProjectAccommodation entity, RoomTypeViewModel owner)
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

            // Extracting list of requests associated with this room
            Requests = owner.Requests.Where(r =>
            {
                bool result = r.RoomId == Id;
                if (result)
                    Occupancy += r.ParticipantsCount;
                return result;
            }).ToList();

            CanManageRooms = owner.CanManageRooms;
            CanAssignRooms = owner.CanAssignRooms;
        }
    }
    
}
