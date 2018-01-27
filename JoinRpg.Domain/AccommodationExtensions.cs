using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
    public static class AccommodationExtensions
    {
        public static int GetRoomFreeSpace(this ProjectAccommodation room)
        {
            return room.ProjectAccommodationType.Capacity - room.GetAllInhabitants().Count();
        }

        public static IEnumerable<Claim> GetAllInhabitants(this ProjectAccommodation room) =>
            room.Inhabitants.SelectMany(i => i.Subjects);

        public static bool IsOccupied(this ProjectAccommodation pa)
        {
            return pa.Inhabitants.Any();
        }
    }
}
