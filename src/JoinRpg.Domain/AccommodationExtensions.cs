namespace JoinRpg.Domain;

public static class AccommodationExtensions
{
    public static IEnumerable<Claim> GetAllInhabitants(this ProjectAccommodation room) =>
        room.Inhabitants.SelectMany(i => i.Subjects);

    /// <summary>
    /// Сколько ещё человек влезет к этой группе: если группа расселена — по её комнате, если нет —
    /// по вместимости выбранного типа проживания.
    /// </summary>
    /// <remarks>
    /// Обслуживает контур приглашений и карточку заявки — он остался на EF-сущностях (ADR018, §13).
    /// Свободное место в комнате для страницы комнат и для заселения считает доменный агрегат —
    /// <c>RoomCategoryPlan.GetFreeSpace</c>; отдельного расчёта «свободное место комнаты» поверх
    /// EF-сущностей больше нет (ADR018, §8).
    /// </remarks>
    public static int GetRoomFreeSpace(this AccommodationRequest accommodationRequest1)
    {
        if (accommodationRequest1.Accommodation is ProjectAccommodation accommodation)
        {
            return accommodation.ProjectAccommodationType.Capacity - accommodation.GetAllInhabitants().Count();
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
