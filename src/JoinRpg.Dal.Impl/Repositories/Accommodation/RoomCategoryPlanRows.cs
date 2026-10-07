namespace JoinRpg.Dal.Impl.Repositories.Accommodation;

/// <summary>
/// Плоская проекция категории комнат для запроса <see cref="RoomCategoryPlanLoader"/>.
/// </summary>
/// <remarks>
/// Обычный класс с <c>init</c>-свойствами, а не позиционный record: EF6 не умеет конструировать
/// record'ы в проекции. По той же причине вложенные коллекции объявлены как
/// <see cref="IEnumerable{T}"/>.
/// </remarks>
internal sealed class RoomCategoryPlanRow
{
    /// <summary>Идентификатор категории — <c>ProjectRoomCategory.Id</c></summary>
    public required int RoomCategoryId { get; init; }

    public required IEnumerable<RoomCategoryPlanRoomRow> Rooms { get; init; }

    public required IEnumerable<RoomCategoryPlanGroupRow> Groups { get; init; }
}

/// <summary>Плоская проекция комнаты. См. замечания к <see cref="RoomCategoryPlanRow"/>.</summary>
internal sealed class RoomCategoryPlanRoomRow
{
    public required int RoomId { get; init; }
    public required string Name { get; init; }
}

/// <summary>
/// Плоская проекция группы жильцов (<c>AccommodationRequest</c>).
/// См. замечания к <see cref="RoomCategoryPlanRow"/>.
/// </summary>
internal sealed class RoomCategoryPlanGroupRow
{
    public required int GroupId { get; init; }
    public required int AccommodationTypeId { get; init; }
    public required int? RoomId { get; init; }
    public required IEnumerable<int> SubjectClaimIds { get; init; }
}
