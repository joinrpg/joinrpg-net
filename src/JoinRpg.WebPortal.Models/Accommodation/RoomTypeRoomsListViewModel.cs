using System.Collections.Generic;

namespace JoinRpg.Web.Models.Accommodation
{

    /// <summary>
    /// View model for list of rooms of a room type
    /// </summary>
    public class RoomTypeRoomsListViewModel
    {

        public RoomTypeRoomsListViewModel(RoomTypeViewModel roomType)
        {
            ProjectId = roomType.ProjectId;
            RoomTypeId = roomType.Id;
            Rooms = roomType.Rooms;
            RoomsCount = roomType.RoomsCount;
            RoomCapacity = roomType.Capacity;
            CanManageRooms = roomType.CanManageRooms;
            CanAssignRooms = roomType.CanAssignRooms;
        }

        public IEnumerable<RoomViewModel> Rooms { get; set; }

        public int ProjectId { get; set; }

        public int RoomTypeId { get; set; }

        public int RoomsCount { get; set; }

        public int RoomCapacity { get; set; }

        public bool CanManageRooms { get; }

        public bool CanAssignRooms { get; }

    }

}
