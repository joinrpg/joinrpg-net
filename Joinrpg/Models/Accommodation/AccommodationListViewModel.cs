using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Accommodation
{
    public class AccommodationListViewModel
    {
        public int ProjectId { get; set; }

        [DisplayName("Проект")]
        public string ProjectName { get; set; }

        [DisplayName("Типы проживания")]
        public IReadOnlyList<RoomTypeViewModel> RoomTypes { get; set; }

        public bool CanAssignRooms { get; set; }
        public bool CanManageRooms { get; set; }

        public AccommodationListViewModel(Project project,
            IReadOnlyCollection<ProjectAccommodationType> roomTypes,
            int userId)
        {
            ProjectId = project.ProjectId;
            ProjectName = project.ProjectName;
            CanManageRooms = project.HasMasterAccess(userId, acl => acl.CanManageAccommodation);
            CanAssignRooms = project.HasMasterAccess(userId, acl => acl.CanSetPlayersAccommodations);
            RoomTypes = roomTypes.Select(rt => new RoomTypeViewModel(rt, userId)).ToList();
        }
    }
}
