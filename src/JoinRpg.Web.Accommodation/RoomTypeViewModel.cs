using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models.Accommodation;

public abstract class RoomTypeViewModelBase
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    /// <summary>
    /// Разумный максимум мест в одном номере: больше похоже на опечатку, а не на настоящий номер.
    /// </summary>
    public const int MaxCapacity = 1000;

    /// <summary>
    /// Чем предзаполнена пустая форма: самый частый номер — двухместный.
    /// </summary>
    public const int DefaultCapacity = 2;

    [DisplayName("Количество мест в номере")]
    [Range(1, MaxCapacity, ErrorMessage = "Укажите количество мест в номере — целое число от 1 до 1000")]
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
    [Required(ErrorMessage = "Укажите название типа поселения")]
    public string Name { get; set; }

    /// <summary>
    /// Описание типа проживания, уже отрендеренное из Markdown — HTML строкой.
    /// </summary>
    /// <remarks>
    /// Именно строка, а не <see cref="MarkupString"/>, потому что эта вью-модель может уехать
    /// в JSON: <see cref="MarkupString"/> — структура без подходящего конструктора, она
    /// сериализуется как <c>{"Value":...}</c> и обратно не десериализуется. В репозитории
    /// принято возить HTML строкой, а типизированное представление помечать
    /// <see cref="JsonIgnoreAttribute"/> — так сделано в <c>AccommodationTypeViewModel</c>
    /// и в вью-моделях сетки ролей.
    /// </remarks>
    public string DescriptionHtml { get; set; } = "";

    /// <summary>
    /// То же описание для вывода в разметке.
    /// </summary>
    /// <remarks>
    /// Значение приходит из <c>MarkdownString.ToHtmlString()</c>, то есть уже прошло наш
    /// санитайзер, и выводится в MVC-разметке через <c>@Html.Raw(...Value)</c>:
    /// <see cref="MarkupString"/> — тип Blazor, в .cshtml он не является <c>IHtmlContent</c>
    /// и сам по себе был бы выведен с HTML-экранированием.
    /// </remarks>
    [DisplayName("Описание")]
    [JsonIgnore]
    public MarkupString DescriptionView => new(DescriptionHtml);

    [DisplayName("Цена за 1 место")]
    [Range(0, int.MaxValue, ErrorMessage = "Цена за 1 место не может быть отрицательной")]
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
    /// <param name="descriptionView">
    /// Описание типа проживания, уже отрендеренное из Markdown вызывающей стороной
    /// (рендерер markdown живёт на сервере, см. <see cref="RoomTypeViewModelBase.DescriptionView"/>).
    /// </param>
    /// <param name="participants">
    /// Жильцы по идентификатору заявки. Денег в плане нет (ADR018, §5) — их считает вью-сервис
    /// страницы одной общей выборкой персонажей.
    /// </param>
    /// <param name="currentUserId">
    /// Пользователь, который смотрит страницу: по нему считаются права
    /// <see cref="RoomTypeViewModelBase.CanManageRooms"/> и
    /// <see cref="RoomTypeViewModelBase.CanAssignRooms"/>.
    /// </param>
    public RoomTypeViewModel(
        RoomCategoryPlan plan,
        AccommodationTypeIdentification typeId,
        MarkupString descriptionView,
        IReadOnlyDictionary<ClaimIdentification, RequestParticipantViewModel> participants,
        UserIdentification currentUserId)
        : this(plan.GetAccommodationType(typeId), descriptionView, currentUserId, plan.ProjectInfo)
    {
        // Creating a list of requests associated with this room type
        Requests = [.. plan.Groups.Select(group => new AccRequestViewModel(
            group,
            [.. group.Subjects.Select(claimId => participants[claimId])]))];

        // Нерасселённые группы: нулевой RoomId — это признак «комната не назначена»
        // (см. AccRequestViewModel.RoomId).
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
        // Группы раскладываются по комнатам здесь, а не внутри RoomViewModel: вью-модели групп уже
        // построены выше, и комната получает свои готовыми, в том же порядке, что и на странице.
        var requestsByRoom = Requests.ToLookup(request => request.RoomId);
        var rl = plan.Rooms
            .Select(room => new RoomViewModel(
                room,
                typeId,
                plan.RoomCapacity,
                [.. requestsByRoom[room.Id.RoomId]],
                CanManageRooms,
                CanAssignRooms))
            .ToList();
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
    /// <param name="descriptionView">
    /// Описание типа проживания, уже отрендеренное из Markdown вызывающей стороной
    /// (рендерер markdown живёт на сервере, см. <see cref="RoomTypeViewModelBase.DescriptionView"/>).
    /// </param>
    /// <param name="currentUserId">
    /// Пользователь, который смотрит страницу: по нему считаются права
    /// <see cref="RoomTypeViewModelBase.CanManageRooms"/> и
    /// <see cref="RoomTypeViewModelBase.CanAssignRooms"/>.
    /// </param>
    public RoomTypeViewModel(
        AccommodationTypeInfo typeInfo,
        MarkupString descriptionView,
        UserIdentification currentUserId,
        ProjectInfo projectInfo)
        : this(currentUserId, projectInfo)
    {
        Id = typeInfo.Id.AccommodationTypeId;
        Cost = typeInfo.Cost;
        Name = typeInfo.Name;
        Capacity = typeInfo.Capacity;
        IsPlayerSelectable = typeInfo.IsPlayerSelectable;
        DescriptionEditable = typeInfo.Description.Value;
        DescriptionHtml = descriptionView.Value;
        Requests = [];
        UnassignedRequests = [];
    }

    /// <summary>
    /// Пустая форма типа проживания. Права на странице считаются по текущему пользователю
    /// <paramref name="currentUserId"/>.
    /// </summary>
    public RoomTypeViewModel(UserIdentification currentUserId, ProjectInfo projectInfo)
    {
        // Пустая форма предзаполняется типовым двухместным номером: иначе в поле отрисуется 0
        // (дефолт int), и первый же сабмит упирается в [Range] на Capacity.
        Capacity = DefaultCapacity;
        ProjectName = projectInfo.ProjectName.Value;
        ProjectId = projectInfo.ProjectId.Value;
        CanManageRooms = projectInfo.HasMasterAccess(currentUserId, Permission.CanManageAccommodation);
        CanAssignRooms = projectInfo.HasMasterAccess(currentUserId, Permission.CanSetPlayersAccommodations);
    }

    public RoomTypeViewModel()
    {
    }
}
