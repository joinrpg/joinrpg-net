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
        // Полная — по правилу свободного места: «Люкс на одного» заполняет двухместную комнату (ADR018).
        var fullRooms = plan.Rooms.Where(room => room.IsOccupied && plan.IsFull(room.Id)).Select(room => room.Id).ToHashSet();
        rooms.Sort((x, y) => CompareRooms(x, y, fullRooms));

        return new RoomTypeRoomsViewModel(
            typeId,
            projectInfo.ProjectName.Value,
            type.Name,
            type.Capacity,
            plan.RoomCapacity,
            [.. plan.AccommodationTypes.Where(t => t.Id != typeId).Select(t => t.Name)],
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
        var type = plan.GetAccommodationType(group.AccommodationTypeId);
        return new AccommodationGroupViewModel(
            group.Id,
            group.AccommodationTypeId,
            type.Capacity,
            // Подпись типа нужна, только когда комнаты общие у нескольких типов (ADR020).
            plan.AccommodationTypes.Count > 1 ? type.Name : null,
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
    private static int CompareRooms(
        AccommodationRoomViewModel x,
        AccommodationRoomViewModel y,
        HashSet<AccommodationRoomIdentification> fullRooms)
    {
        // Сначала полнота: в общем пуле одинаково занятые комнаты бывают и полной, и нет (ADR018).
        var xFull = fullRooms.Contains(x.RoomId);
        var yFull = fullRooms.Contains(y.RoomId);
        if (xFull != yFull)
        {
            return xFull ? 1 : -1;
        }

        var xOccupancy = x.Groups.Sum(g => g.Persons);
        var yOccupancy = y.Groups.Sum(g => g.Persons);
        if (xOccupancy == yOccupancy)
        {
            // Числовые номера — по числу и раньше прочих: смешивать числовое и строковое сравнение
            // нельзя, порядок «2 < 10 < 1а < 2» нетранзитивен.
            var xIsNumber = int.TryParse(x.Name, out var xn);
            var yIsNumber = int.TryParse(y.Name, out var yn);
            if (xIsNumber != yIsNumber)
            {
                return xIsNumber ? -1 : 1;
            }

            return xIsNumber
                ? xn.CompareTo(yn)
                : string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        return yOccupancy - xOccupancy;
    }
}
