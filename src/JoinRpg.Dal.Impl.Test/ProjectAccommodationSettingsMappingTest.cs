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

    [Fact]
    public void GetTypeById_ThrowsForUnknownId()
    {
        _ = Should.Throw<KeyNotFoundException>(
            () => mock.ProjectInfo.AccommodationSettings.GetTypeById(
                new AccommodationTypeIdentification(mock.ProjectInfo.ProjectId, 100500)));
    }
}
