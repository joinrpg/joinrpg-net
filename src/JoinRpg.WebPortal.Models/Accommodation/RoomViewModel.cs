using JoinRpg.DomainTypes.Accommodation;

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
    /// Комната поверх доменного агрегата плана поселения (ADR018). Вместимость у комнаты не
    /// своя — она общая для пула, поэтому берётся у типа проживания.
    /// </summary>
    public RoomViewModel(RoomInfo room, RoomTypeViewModel owner)
    {
        Id = room.Id.RoomId;
        Name = room.Name;
        ProjectId = room.Id.ProjectId.Value;
        RoomTypeId = owner.Id;
        Capacity = owner.Capacity;

        // Группы проживающих, расселённые именно в эту комнату
        Requests = [.. owner.Requests.Where(r => r.RoomId == Id)];
        Occupancy = Requests.Sum(r => r.Persons);

        CanManageRooms = owner.CanManageRooms;
        CanAssignRooms = owner.CanAssignRooms;
    }
}

