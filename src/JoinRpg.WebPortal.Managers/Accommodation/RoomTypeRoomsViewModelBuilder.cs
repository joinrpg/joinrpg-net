using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.WebPortal.Managers.Accommodation;

/// <summary>
/// Жилец вместе с балансом его заявки. Денег в плане поселения нет (ADR018, §5), поэтому они
/// приходят снаружи — из агрегата персонажа.
/// </summary>
internal record RoomResidentWithFee(ClaimLinkViewModel Resident, int FeeTotal, int FeeDue);

/// <summary>
/// Вью-модель страницы «Комнаты» из плана поселения (ADR018).
/// </summary>
internal static class RoomTypeRoomsViewModelBuilder
{
    /// <param name="plan">План категории комнат, из которой селится тип проживания</param>
    /// <param name="typeId">Тип проживания, чья это страница</param>
    /// <param name="residents">Жильцы с балансом по идентификатору заявки</param>
    /// <param name="currentUserId">Кто смотрит страницу: по нему считаются права</param>
    public static RoomTypeRoomsViewModel Build(
        RoomCategoryPlan plan,
        AccommodationTypeIdentification typeId,
        IReadOnlyDictionary<ClaimIdentification, RoomResidentWithFee> residents,
        UserIdentification currentUserId)
    {
        var type = plan.GetAccommodationType(typeId);
        var projectInfo = plan.ProjectInfo;

        var groups = plan.Groups.ToDictionary(g => g.Id, g => BuildGroup(plan, g, residents));

        var unassigned = plan.UnassignedGroups.Select(g => groups[g.Id]).ToList();
        unassigned.Sort(CompareUnassigned);

        var rooms = plan.Rooms
            .Select(room => new AccommodationRoomViewModel(
                room.Id,
                room.Name,
                // Группы комнаты — в том же порядке, что в плане, как и раньше.
                [.. plan.Groups.Where(g => g.RoomId == room.Id).Select(g => groups[g.Id])]))
            .ToList();
        rooms.Sort((x, y) => CompareRooms(x, y, plan.RoomCapacity));

        return new RoomTypeRoomsViewModel(
            typeId,
            projectInfo.ProjectName.Value,
            type.Name,
            type.Capacity,
            plan.RoomCapacity,
            rooms,
            unassigned,
            CanManageRooms: projectInfo.HasMasterAccess(currentUserId, Permission.CanManageAccommodation),
            CanAssignRooms: projectInfo.HasMasterAccess(currentUserId, Permission.CanSetPlayersAccommodations));
    }

    private static AccommodationGroupViewModel BuildGroup(
        RoomCategoryPlan plan,
        AccommodationGroupInfo group,
        IReadOnlyDictionary<ClaimIdentification, RoomResidentWithFee> residents)
    {
        var members = group.Subjects.Select(claimId => residents[claimId]).ToList();
        var feeToPay = members.Sum(m => m.FeeDue);
        return new AccommodationGroupViewModel(
            group.Id,
            group.AccommodationTypeId,
            plan.GetAccommodationType(group.AccommodationTypeId).Capacity,
            [.. members.Select(m => m.Resident)],
            FeeTotal: members.Sum(m => m.FeeTotal),
            // Переплата долгом не считается.
            FeeToPay: Math.Max(0, feeToPay));
    }

    /// <summary>Нерасселённые: сначала маленькие группы, потом меньший долг, потом по именам</summary>
    private static int CompareUnassigned(AccommodationGroupViewModel x, AccommodationGroupViewModel y)
    {
        var result = x.Persons - y.Persons;
        if (result == 0)
        {
            result = x.FeeToPay - y.FeeToPay;
        }

        if (result == 0)
        {
            result = string.Compare(x.PersonsList, y.PersonsList, StringComparison.CurrentCultureIgnoreCase);
        }

        return result;
    }

    /// <summary>
    /// Комнаты: сначала самые населённые, заполненные — в конец; при равной занятости — по номеру.
    /// </summary>
    private static int CompareRooms(AccommodationRoomViewModel x, AccommodationRoomViewModel y, int capacity)
    {
        var xOccupancy = x.Groups.Sum(g => g.Persons);
        var yOccupancy = y.Groups.Sum(g => g.Persons);
        if (xOccupancy == yOccupancy)
        {
            if (int.TryParse(x.Name, out var xn) && int.TryParse(y.Name, out var yn))
            {
                return xn - yn;
            }

            return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        if (xOccupancy == capacity)
        {
            return 1;
        }

        if (yOccupancy == capacity)
        {
            return -1;
        }

        return yOccupancy - xOccupancy;
    }
}
