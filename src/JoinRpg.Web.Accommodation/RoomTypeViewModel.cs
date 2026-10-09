using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models.Accommodation;

public abstract class RoomTypeViewModelBase
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    [DisplayName("Количество мест в номере")]
    public int Capacity { get; set; }

    /// <summary>
    /// Бесконечное поселение. Значение приходит из метаданных проекта
    /// (<see cref="AccommodationTypeInfo.IsInfinite"/>), где функциональность объявлена
    /// нереализованной.
    /// </summary>
    /// <remarks>
    /// Сеттер закрыт: флаг ставят только конструкторы наследников из
    /// <see cref="AccommodationTypeInfo"/>. Это вью-модель только для показа: форма
    /// редактирования типа (<see cref="JoinRpg.Web.Accommodation.RoomTypeEditForm"/>) работает
    /// со своей моделью, где нереализованных флагов нет вовсе.
    /// </remarks>
    [DisplayName("Бесконечное поселение")]
    public bool IsInfinite { get; private protected init; } = false;

    [Display(Name = "Игроки могут выбрать данный тип проживания",
        Description = "Если снять этот флаг, то только мастер может назначать этот тип поселения игрокам")]
    public bool IsPlayerSelectable { get; set; } = true;

    /// <summary>
    /// Автозаполнение комнат. Значение приходит из метаданных проекта
    /// (<see cref="AccommodationTypeInfo.IsAutoFilledAccommodation"/>), где функциональность
    /// объявлена нереализованной.
    /// </summary>
    /// <remarks>
    /// Сеттер закрыт по тем же причинам, что у <see cref="IsInfinite"/>.
    /// </remarks>
    [DisplayName("Автозаполнение")]
    public bool IsAutoFilledAccommodation { get; private protected init; } = false;

    [DisplayName("Название")]
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
    public int Cost { get; set; }

    public bool CanAssignRooms { get; set; }
    public bool CanManageRooms { get; set; }
}

//todo I18n
public class RoomTypeViewModel : RoomTypeViewModelBase
{
    public string ProjectName { get; set; }

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
        // Нереализованные флаги берём из метаданных явно, а не оставляем в дефолте вью-модели:
        // в метаданных они всегда false (см. AccommodationTypeInfo.IsInfinite), и форма должна
        // показывать именно это значение, а не совпадающий с ним по случайности дефолт.
        IsInfinite = typeInfo.IsInfinite;
        IsAutoFilledAccommodation = typeInfo.IsAutoFilledAccommodation;
        DescriptionHtml = descriptionView.Value;
    }

    /// <summary>
    /// Общая часть: проект и права на странице, которые считаются по текущему пользователю
    /// <paramref name="currentUserId"/>.
    /// </summary>
    private RoomTypeViewModel(UserIdentification currentUserId, ProjectInfo projectInfo)
    {
        ProjectName = projectInfo.ProjectName.Value;
        ProjectId = projectInfo.ProjectId.Value;
        CanManageRooms = projectInfo.HasMasterAccess(currentUserId, Permission.CanManageAccommodation);
        CanAssignRooms = projectInfo.HasMasterAccess(currentUserId, Permission.CanSetPlayersAccommodations);
    }
}
