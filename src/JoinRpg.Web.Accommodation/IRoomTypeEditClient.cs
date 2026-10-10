namespace JoinRpg.Web.Accommodation;

/// <summary>
/// Форма создания и изменения типа проживания — то, что мастер вводит руками.
/// </summary>
/// <remarks>
/// Класс с сеттерами, а не record: его правит <c>EditForm</c>. Нереализованных флагов
/// (бесконечное поселение, автозаполнение) здесь нет: в форме они не показываются, а сервис их
/// не принимает (см. <c>AccommodationTypeInfo.IsInfinite</c>).
/// </remarks>
public class RoomTypeEditViewModel : IValidatableObject
{
    /// <summary>
    /// Разумный максимум мест в одном номере: больше похоже на опечатку, а не на настоящий номер.
    /// </summary>
    public const int MaxCapacity = 1000;

    /// <summary>
    /// Чем предзаполнена пустая форма: самый частый номер — двухместный.
    /// </summary>
    public const int DefaultCapacity = 2;

    [Display(Name = "Название")]
    [Required(ErrorMessage = "Укажите название типа поселения")]
    public string Name { get; set; } = "";

    [Display(Name = "Цена за 1 место")]
    [Range(0, int.MaxValue, ErrorMessage = "Цена за 1 место не может быть отрицательной")]
    public int Cost { get; set; }

    /// <remarks>
    /// Пустая форма предзаполняется типовым двухместным номером: иначе в поле отрисуется 0
    /// (дефолт int), и первый же сабмит упирается в <see cref="RangeAttribute"/>.
    /// </remarks>
    [Display(Name = "Количество мест в номере")]
    [Range(1, MaxCapacity, ErrorMessage = "Укажите количество мест в номере — целое число от 1 до 1000")]
    public int Capacity { get; set; } = DefaultCapacity;

    [Display(Name = "Игроки могут выбрать данный тип проживания",
        Description = "Если снять этот флаг, то только мастер может назначать этот тип поселения игрокам")]
    public bool IsPlayerSelectable { get; set; } = true;

    /// <summary>
    /// Описание в Markdown — как его ввёл мастер, не отрендеренное.
    /// </summary>
    [Display(Name = "Описание")]
    public string Description { get; set; } = "";

    /// <summary>
    /// Новая категория комнат или существующая — выбирается только при создании типа (ADR020):
    /// перенос типа в другую категорию означал бы переселение его жильцов.
    /// </summary>
    [Display(Name = "Категория комнат")]
    public RoomCategoryChoice RoomCategoryChoice { get; set; } = RoomCategoryChoice.New;

    /// <summary>Название новой категории; пустое — категория получит имя типа</summary>
    [Display(Name = "Название новой категории")]
    public string? NewRoomCategoryName { get; set; }

    [Display(Name = "Категория")]
    public int? ExistingRoomCategoryId { get; set; }

    /// <summary>Категории проекта — справочно, для выбора существующей</summary>
    public IReadOnlyList<RoomCategoryOptionViewModel> RoomCategories { get; set; } = [];

    /// <summary>
    /// Категория уже созданного типа — справочно; при создании <c>null</c>. Её
    /// <see cref="RoomCategoryOptionViewModel.TypeNames"/> — только соседние типы, без самого изменяемого.
    /// </summary>
    public RoomCategoryOptionViewModel? RoomCategory { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RoomCategoryChoice == RoomCategoryChoice.Existing && ExistingRoomCategoryId is null)
        {
            yield return new ValidationResult(
                "Выберите категорию комнат или заведите новую",
                [nameof(ExistingRoomCategoryId)]);
        }
    }
}

/// <summary>Как выбрать категорию комнат для нового типа проживания (ADR020)</summary>
public enum RoomCategoryChoice
{
    New,
    Existing,
}

/// <summary>Категория комнат в форме типа проживания</summary>
/// <param name="Id">Номер категории в проекте</param>
/// <param name="Name">Название категории</param>
/// <param name="TypeNames">Типы проживания, которые уже селятся из этой категории</param>
public record RoomCategoryOptionViewModel(int Id, string Name, IReadOnlyList<string> TypeNames);

/// <summary>
/// Создание и изменение типа проживания мастером.
/// </summary>
/// <remarks>
/// Отдельно от <see cref="IAccommodationTypeClient"/>: тот — выбор типа игроком в заявке, у него
/// другая аудитория и другие права.
/// </remarks>
public interface IRoomTypeEditClient
{
    /// <summary>Текущие параметры типа проживания для формы изменения</summary>
    Task<RoomTypeEditViewModel> GetRoomType(AccommodationTypeIdentification roomTypeId);

    /// <summary>Пустая форма нового типа — со списком категорий комнат проекта</summary>
    Task<RoomTypeEditViewModel> GetNewRoomType(ProjectIdentification projectId);

    /// <summary>Создать тип проживания</summary>
    Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model);

    /// <summary>Сохранить изменения типа проживания</summary>
    Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model);
}

/// <summary>
/// Ссылки на страницы раздела «Поселение». Реализаций две — серверная (<c>UriServiceImpl</c>)
/// и клиентская (<c>UriLocatorExtensions</c>); что они не разошлись, проверяет
/// <c>UriLocatorConsistencyTests</c>.
/// </summary>
public interface IAccommodationUriLocator
{
    /// <summary>Список типов проживания проекта</summary>
    Uri GetRoomTypesListUri(ProjectIdentification projectId);
}
