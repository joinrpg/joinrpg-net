using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Маппинг типов проживания в <c>ProjectInfo</c> (ADR015). БД не используется — только
/// in-memory граф <c>Project</c> из мока.
/// </summary>
public class ProjectAccommodationSettingsMappingTest
{
    private readonly MockedProject mock = new();

    [Fact]
    public void ByDefault_NoTypes_AndDisabled()
    {
        mock.ProjectInfo.AccommodationSettings.Enabled.ShouldBeFalse();
        mock.ProjectInfo.AccommodationSettings.Types.ShouldBeEmpty();
    }

    [Fact]
    public void AccommodationTypes_AreMappedToProjectInfo()
    {
        var tent = mock.CreateAccommodationType("Палатка", capacity: 4, cost: 1500);
        _ = mock.CreateAccommodationType("Люкс", capacity: 2, cost: 9000, isPlayerSelectable: false);
        mock.Project.Details.EnableAccommodation = true;

        mock.ReInitProjectInfo();

        var settings = mock.ProjectInfo.AccommodationSettings;
        settings.Enabled.ShouldBeTrue();
        settings.Types.Count.ShouldBe(2);

        var tentInfo = settings.GetTypeById(
            new AccommodationTypeIdentification(mock.ProjectInfo.ProjectId, tent.Id));
        tentInfo.Name.ShouldBe("Палатка");
        tentInfo.Cost.ShouldBe(1500);
        tentInfo.Capacity.ShouldBe(4);
        tentInfo.IsPlayerSelectable.ShouldBeTrue();
        // Описания в БД нет — домен получает пустой markdown, а не null
        tentInfo.Description.ShouldBe(new MarkdownString(""));

        settings.PlayerSelectableTypes.Select(t => t.Name).ShouldBe(["Палатка"]);
    }

    /// <summary>
    /// Тест-страж (ADR018, «Задел на разделение», пункт 1). Категория комнат и тип проживания
    /// сегодня не разделены: своей таблицы у категории нет, и маппер заполняет
    /// <c>RoomCategoryId</c> из <c>ProjectAccommodationType.Id</c>, то есть числа совпадают.
    /// В день, когда категорию разделят с типом, этот тест упадёт первым — и это правильно:
    /// он приведёт разработчика в раздел 2 ADR018, а не в случайное место, где идентификаторы
    /// молча разъехались. Тогда его надо не «починить», а удалить вместе с фикцией.
    /// </summary>
    [Fact]
    public void RoomCategoryId_TodayEqualsAccommodationTypeId()
    {
        _ = mock.CreateAccommodationType("Палатка", capacity: 4, cost: 1500);
        _ = mock.CreateAccommodationType("Люкс", capacity: 2, cost: 9000);
        mock.Project.Details.EnableAccommodation = true;

        mock.ReInitProjectInfo();

        var types = mock.ProjectInfo.AccommodationSettings.Types;
        types.ShouldNotBeEmpty();
        foreach (var type in types)
        {
            type.RoomCategoryId.ProjectId.ShouldBe(type.Id.ProjectId);
            type.RoomCategoryId.RoomCategoryId.ShouldBe(type.Id.AccommodationTypeId);
        }
    }

    [Fact]
    public void GetTypeById_ThrowsForUnknownId()
    {
        _ = Should.Throw<AccommodationTypeNotFoundException>(
            () => mock.ProjectInfo.AccommodationSettings.GetTypeById(
                new AccommodationTypeIdentification(mock.ProjectInfo.ProjectId, 100500)));
    }
}
