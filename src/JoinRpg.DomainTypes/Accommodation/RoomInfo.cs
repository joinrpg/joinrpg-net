using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.DomainTypes.Accommodation;

/// <summary>
/// Комната. Вместимости у комнаты нет — она общая для пула,
/// см. <see cref="RoomCategoryPlan.RoomCapacity"/>.
/// </summary>
/// <param name="Id">Идентификатор комнаты</param>
/// <param name="Name">Название (номер) комнаты</param>
/// <param name="Inhabitants">Группы, поселённые в эту комнату</param>
public record class RoomInfo(
    AccommodationRoomIdentification Id,
    string Name,
    IReadOnlyCollection<AccommodationGroupInfo> Inhabitants)
{
    /// <summary>Сколько человек сейчас живёт в комнате</summary>
    public int Occupancy => Inhabitants.Sum(i => i.SubjectsCount);

    /// <summary>Заселена ли комната хоть кем-то. Удалить можно только незаселённую комнату.</summary>
    public bool IsOccupied => Inhabitants.Count > 0;
}
