using System.ComponentModel;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Accommodation
{
    public class AccommodationListViewModel
    {
        public int ProjectId { get; set; }

        [DisplayName("Проект")]
        public string ProjectName { get; set; }

        [DisplayName("Типы проживания")]
        public List<RoomTypeListItemViewModel> RoomTypes { get; set; }

        public bool CanAssignRooms { get; set; }
        public bool CanManageRooms { get; set; }

        public int? TotalCapacity { get; set; }

        public int? FreeCapacity { get; set; }

        public int TotalOccupied { get; set; }

        public int TotalPending { get; set; }

        public bool IsInfinite { get; set; }

        public AccommodationListViewModel(Project project,
            IReadOnlyCollection<RoomTypeInfoRow> roomTypes,
            int userId)
        {
            ProjectId = project.ProjectId;
            ProjectName = project.ProjectName;
            CanManageRooms = project.HasMasterAccess(userId, acl => acl.CanManageAccommodation);
            CanAssignRooms = project.HasMasterAccess(userId, acl => acl.CanSetPlayersAccommodations);
            RoomTypes = roomTypes.Select(rt => new RoomTypeListItemViewModel(rt, userId)).ToList();

            IsInfinite = RoomTypes.Any(rt => rt.IsInfinite);

            var allInfinite = RoomTypes.All(rt => rt.IsInfinite);
            TotalCapacity = allInfinite ? (int?)null : RoomTypes.Sum(rt => rt.TotalCapacity);
            FreeCapacity = allInfinite ? (int?)null : RoomTypes.Sum(rt => rt.FreeCapacity);

            TotalOccupied = RoomTypes.Sum(x => x.Occupied);
            TotalPending = RoomTypes.Sum(x => x.PendingRequests);
        }
    }

    public class RoomTypeListItemViewModel : RoomTypeViewModelBase
    {
        [DisplayName("Проживает")]
        public int Occupied { get; }

        public int PendingRequests { get; }

        public override int RoomsCount { get; }

        public RoomTypeListItemViewModel(RoomTypeInfoRow row, int userId)
        {
            var entity = row.RoomType;
            var project = row.RoomType.Project;
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }
            Id = entity.Id;
            Cost = entity.Cost;
            Name = entity.Name;
            Capacity = entity.Capacity;
            IsInfinite = entity.IsInfinite;

            IsPlayerSelectable = entity.IsPlayerSelectable;
            IsAutoFilledAccommodation = entity.IsAutoFilledAccommodation;
            DescriptionView = entity.Description.ToHtmlString();
            ProjectId = project.ProjectId;
            CanManageRooms = project.HasMasterAccess(userId, acl => acl.CanManageAccommodation);
            CanAssignRooms = project.HasMasterAccess(userId, acl => acl.CanSetPlayersAccommodations);

            Occupied = row.Occupied;
            RoomsCount = row.RoomsCount;
            ApprovedClaims = row.ApprovedClaims;

            FreeCapacity = TotalCapacity - Occupied;
            PendingRequests = ApprovedClaims - Occupied;
        }

        public int ApprovedClaims { get; set; }
        public int FreeCapacity { get; }
    }
}
