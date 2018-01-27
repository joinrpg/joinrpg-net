using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Email
{
    public class RoomEmailBase : EmailModelBase
    {
        public AccommodationRequest ChangedRequest { get; set; }
        public ProjectAccommodation Room { get; set; }
    }

    public class OccupyRoomEmail : RoomEmailBase
    {
    }

    public class UnOccupyRoomEmail : RoomEmailBase
    {
    }

    public class LeaveRoomEmail : RoomEmailBase
    {
        public Claim Claim { get; set; }
    }
}
