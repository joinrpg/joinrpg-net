using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.DomainTypes.ProjectMetadata.Accommodation;

/// <summary>
/// Тип проживания в проекте (палатка, домик, номер в отеле…) — настройка мастера, часть
/// метаданных проекта (ADR015).
/// </summary>
/// <param name="Id">Идентификатор типа проживания</param>
/// <param name="RoomCategoryId">
/// Категория комнат (пул), из которого селится этот тип проживания. Единственный способ узнать
/// пул по типу проживания — конвертации идентификаторов в домене нет (ADR018, «Задел на
/// разделение»).
/// </param>
/// <param name="Name">Название типа, как его видит игрок</param>
/// <param name="Description">Описание типа проживания</param>
/// <param name="Cost">Стоимость проживания для одного игрока</param>
/// <param name="Capacity">Сколько игроков помещается в одну комнату этого типа</param>
/// <param name="IsPlayerSelectable">Может ли игрок сам выбрать этот тип в своей заявке</param>
public record AccommodationTypeInfo(
    AccommodationTypeIdentification Id,
    RoomCategoryIdentification RoomCategoryId,
    string Name,
    MarkdownString Description,
    int Cost,
    int Capacity,
    bool IsPlayerSelectable)
{
    /// <summary>
    /// Бесконечное поселение: тип, в который можно селить сколько угодно игроков, не заводя комнат
    /// (например, «своя палатка»). <b>Функциональность не реализована</b>, поэтому свойство всегда
    /// возвращает <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Колонка <c>IsInfinite</c> в таблице типов проживания есть и остаётся — ADR015 переносил
    /// настройки типа в метаданные, но колонку не удалял; на проде значение <c>true</c> стоит
    /// у единиц строк в архивных проектах. Записать флаг нечем: <c>AccommodationTypeRequest</c>
    /// такого поля не несёт, а чекбоксы в форме редактирования типа закомментированы.
    /// Поэтому здесь объявлено именно свойство-заглушка, а не параметр записи, читаемый из
    /// колонки: «не реализовано» должно быть видно в доменной модели, чтобы потребители
    /// метаданных не оставляли флаг в неявном дефолте своей вью-модели (ADR015).
    /// Когда функциональность реализуют, свойство станет настоящим — параметром записи,
    /// читаемым из колонки, — и потребителей менять не придётся.
    /// </remarks>
    public bool IsInfinite => false;

    /// <summary>
    /// Автозаполнение: тип проживания, по которому игроки расселяются по комнатам автоматически.
    /// <b>Функциональность не реализована</b>, поэтому свойство всегда возвращает <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Всё сказанное про <see cref="IsInfinite"/> верно и здесь: колонка
    /// <c>IsAutoFilledAccommodation</c> в БД есть и остаётся, а записать её нечем.
    /// </remarks>
    public bool IsAutoFilledAccommodation => false;
}

/// <summary>
/// Настройки проживания проекта: включён ли модуль и какие типы проживания заведены.
/// </summary>
/// <param name="Enabled">Включён ли модуль проживания в проекте</param>
/// <param name="Types">Все типы проживания проекта</param>
public record ProjectAccommodationSettings(
    bool Enabled,
    IReadOnlyCollection<AccommodationTypeInfo> Types)
{
    /// <summary>Тип проживания по идентификатору</summary>
    /// <exception cref="AccommodationTypeNotFoundException">Типа проживания с таким идентификатором нет</exception>
    public AccommodationTypeInfo GetTypeById(AccommodationTypeIdentification id)
    {
        return GetTypeByIdOrDefault(id)
            ?? throw new AccommodationTypeNotFoundException(id);
    }

    /// <summary>
    /// Тип проживания по идентификатору или <c>null</c>, если такого нет. Для мест, где
    /// идентификатор приходит параметром фильтра и промах — это 404, а не ошибка.
    /// </summary>
    public AccommodationTypeInfo? GetTypeByIdOrDefault(AccommodationTypeIdentification id)
        => Types.SingleOrDefault(t => t.Id == id);

    /// <summary>Типы, доступные игроку для самостоятельного выбора</summary>
    public IReadOnlyCollection<AccommodationTypeInfo> PlayerSelectableTypes
        => [.. Types.Where(t => t.IsPlayerSelectable)];
}
