using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Accommodation;

public abstract class RoomTypeViewModelBase
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    [DisplayName("Количество мест в номере")]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    [DisplayName("Бесконечное поселение")]
    public bool IsInfinite { get; set; } = false;

    [Display(Name = "Игроки могут выбрать данный тип проживания",
        Description = "Если снять этот флаг, то только мастер может назначать этот тип поселения игрокам")]
    public bool IsPlayerSelectable { get; set; } = true;

    [DisplayName("Автозаполнение")]
    public bool IsAutoFilledAccommodation { get; set; } = false;

    public abstract int RoomsCount { get; }

    [DisplayName("Общее количество мест")]
    public int TotalCapacity
        => RoomsCount * Capacity;

    [DisplayName("Название")]
    [Required]
    public string Name { get; set; }

    [DisplayName("Описание")]
    public JoinHtmlString DescriptionView { get; set; }

    [DisplayName("Цена за 1 место")]
    public int Cost { get; set; }

    public bool CanAssignRooms { get; set; }
    public bool CanManageRooms { get; set; }
}

//todo I18n
public class RoomTypeViewModel : RoomTypeViewModelBase
{
    [DisplayName("Описание"), UIHint("MarkdownString")]
    public string DescriptionEditable { get; set; }

    public string ProjectName { get; set; }


    /// <summary>
    /// List of rooms
    /// </summary>
    public IReadOnlyList<RoomViewModel> Rooms { get; }

    public override int RoomsCount
        => Rooms?.Count ?? 0;

    /// <summary>
    /// List of requests sent for this room type
    /// </summary>
    public IReadOnlyList<AccRequestViewModel> Requests { get; set; }

    /// <summary>
    /// List of requests not assigned to any room
    /// </summary>
    public IReadOnlyList<AccRequestViewModel> UnassignedRequests { get; set; }

    /// <summary>
    /// Страница комнат: комнаты и группы проживающих берутся из доменного агрегата плана
    /// поселения (ADR018), настройки типа — из метаданных проекта (ADR015).
    /// </summary>
    /// <param name="plan">План категории комнат, из которой селится этот тип проживания</param>
    /// <param name="typeId">Тип проживания, чью страницу показываем</param>
    /// <param name="participants">
    /// Жильцы по идентификатору заявки. Денег в плане нет (ADR018, §5) — их считает вью-сервис
    /// страницы одной общей выборкой персонажей.
    /// </param>
    public RoomTypeViewModel(
        RoomCategoryPlan plan,
        AccommodationTypeIdentification typeId,
        IReadOnlyDictionary<ClaimIdentification, RequestParticipantViewModel> participants,
        UserIdentification userId)
        : this(plan.GetAccommodationType(typeId), userId, plan.ProjectInfo)
    {
        // Creating a list of requests associated with this room type
        Requests = [.. plan.Groups.Select(group => new AccRequestViewModel(
            group,
            [.. group.Subjects.Select(claimId => participants[claimId])]))];

        // Creating a list of requests not assigned to any room
        var ua = Requests.Where(ar => ar.RoomId == 0).ToList();
        ua.Sort((x, y) =>
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
        });
        UnassignedRequests = ua;

        // Creating a list of rooms contained in this room type
        var rl = plan.Rooms.Select(room => new RoomViewModel(room, this)).ToList();
        rl.Sort((x, y) =>
        {
            if (x.Occupancy == y.Occupancy)
            {
                if (int.TryParse(x.Name, out var xn) && int.TryParse(y.Name, out var yn))
                {
                    return xn - yn;
                }

                return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
            }
            if (x.Occupancy == x.Capacity)
            {
                return 1;
            }

            if (y.Occupancy == y.Capacity)
            {
                return -1;
            }

            return y.Occupancy - x.Occupancy;
        });
        Rooms = rl;
    }

    /// <summary>
    /// Форма редактирования типа проживания: берёт настройку из метаданных проекта (ADR015).
    /// Комнаты и заявки в метаданные не входят и здесь не нужны — их показывает отдельная
    /// страница «Комнаты».
    /// </summary>
    public RoomTypeViewModel(AccommodationTypeInfo typeInfo, UserIdentification userId, ProjectInfo projectInfo)
        : this(userId, projectInfo)
    {
        Id = typeInfo.Id.AccommodationTypeId;
        Cost = typeInfo.Cost;
        Name = typeInfo.Name;
        Capacity = typeInfo.Capacity;
        IsPlayerSelectable = typeInfo.IsPlayerSelectable;
        DescriptionEditable = typeInfo.Description.Value;
        DescriptionView = typeInfo.Description.ToHtmlString();
        Requests = [];
        UnassignedRequests = [];
    }

    public RoomTypeViewModel(UserIdentification userId, ProjectInfo projectInfo)
    {
        ProjectName = projectInfo.ProjectName.Value;
        ProjectId = projectInfo.ProjectId.Value;
        CanManageRooms = projectInfo.HasMasterAccess(userId, Permission.CanManageAccommodation);
        CanAssignRooms = projectInfo.HasMasterAccess(userId, Permission.CanSetPlayersAccommodations);
    }

    public RoomTypeViewModel()
    {
    }
}
