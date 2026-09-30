using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// View model for single room
/// </summary>
public class RoomViewModel
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }

    public int ProjectId { get; set; }

    [Required]
    [DisplayName("Название (номер)")]
    public string Name { get; set; }

    //public virtual ICollection<ClaimViewModel> Inhabitants { get; set; }

    public IReadOnlyList<AccRequestViewModel> Requests { get; set; }

    public int Capacity { get; set; }

    public int Occupancy { get; private set; }

    public bool CanManageRooms { get; }

    public bool CanAssignRooms { get; set; }

    /// <summary>
    /// Комната поверх доменного агрегата плана поселения (ADR018).
    /// </summary>
    /// <param name="room">Комната из плана — она несёт только имя и своих жильцов</param>
    /// <param name="roomTypeId">Тип проживания, чью страницу показываем</param>
    /// <param name="roomCapacity">
    /// Вместимость комнаты. Своей вместимости у комнаты нет, это свойство пула —
    /// <see cref="RoomCategoryPlan.RoomCapacity"/>.
    /// </param>
    /// <param name="requests">
    /// Группы проживающих, расселённые именно в эту комнату, в том же порядке, что и на всей
    /// странице. Готовые вью-модели приходят снаружи: они же показываются в списке нерасселённых,
    /// и строить их второй раз незачем.
    /// </param>
    /// <param name="canManageRooms">Право заводить, переименовывать и удалять комнаты</param>
    /// <param name="canAssignRooms">Право расселять и выселять игроков</param>
    /// <remarks>
    /// Вью-модель типа проживания сюда сознательно не передаётся: комнате нужны ровно эти данные,
    /// а не весь владелец с его коллекциями, и от порядка конструирования она не зависит.
    /// </remarks>
    public RoomViewModel(
        RoomInfo room,
        AccommodationTypeIdentification roomTypeId,
        int roomCapacity,
        IReadOnlyList<AccRequestViewModel> requests,
        bool canManageRooms,
        bool canAssignRooms)
    {
        Id = room.Id.RoomId;
        Name = room.Name;
        ProjectId = room.Id.ProjectId.Value;
        RoomTypeId = roomTypeId.AccommodationTypeId;
        Capacity = roomCapacity;

        Requests = requests;
        Occupancy = room.Occupancy;

        CanManageRooms = canManageRooms;
        CanAssignRooms = canAssignRooms;
    }
}
