namespace JoinRpg.Domain;

public static class AccommodationExtensions
{
    public static int GetRoomFreeSpace(this ProjectAccommodation room) => room.ProjectAccommodationType.Capacity - room.GetAllInhabitants().Count();

    public static IEnumerable<Claim> GetAllInhabitants(this ProjectAccommodation room) =>
        room.Inhabitants.SelectMany(i => i.Subjects);

    public static bool IsOccupied(this ProjectAccommodation pa) => pa.Inhabitants.Any();

    public static int GetRoomFreeSpace(this AccommodationRequest accommodationRequest1)
    {
        if (accommodationRequest1.Accommodation is ProjectAccommodation accommodation)
        {
            return accommodation.GetRoomFreeSpace();
        }
        else
        {
            return accommodationRequest1.AccommodationType.Capacity - accommodationRequest1.Subjects.Count;
        }
    }

    public static List<User> GetClaimNeighbours(this Claim claim)
    {
        if (claim.AccommodationRequest is AccommodationRequest accommodationRequest)
        {
            if (claim.AccommodationRequest.Accommodation is ProjectAccommodation accommodation)
            {
                return [.. accommodation.Inhabitants.SelectMany(i => i.Subjects).Where(s => s.ClaimId != claim.ClaimId).Select(c => c.Player)];
            }
            else
            {
                return [.. accommodationRequest.Subjects.Where(s => s.ClaimId != claim.ClaimId).Select(c => c.Player)];
            }
        }
        else
        {
            return [];
        }
    }
}
