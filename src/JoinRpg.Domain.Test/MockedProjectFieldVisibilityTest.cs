using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Набор комбинаций «видимость поля + право игрока менять», которые <see cref="MockedProject"/>
/// обязан воспроизводить.
/// </summary>
/// <remarks>
/// Прод хранит три независимых флага (<c>ProjectField.IsPublic</c>, <c>CanPlayerView</c>,
/// <c>CanPlayerEdit</c>), а <see cref="ProjectFieldVisibility"/> выводится из первых двух
/// (<c>ProjectMetadataRepository.CreateProjectFieldVisibility</c>). Единственная запрещённая
/// комбинация — «игрок может менять поле, которого не видит»
/// (<c>GameFieldViewModelBase.Validate</c>). Мок раньше запрещал заодно и «публичное + игрок
/// может менять», из-за чего реальный баг потери значения такого поля в непринятой заявке жил
/// незамеченным.
/// </remarks>
public class MockedProjectFieldVisibilityTest
{
    private readonly MockedProject mock = new();

    [Fact]
    public void PublicFieldCanBeEditableByPlayer()
    {
        var field = mock.CreateField(
            "Публичное, игрок может менять",
            ProjectFieldVisibility.Public,
            canPlayerEdit: true,
            showOnUnApprovedClaims: true);

        field.IsPublic.ShouldBeTrue();
        field.CanPlayerEdit.ShouldBeTrue();

        // Публичное поле видно любому зрителю, даже анонимному.
        field.HasViewAccess(AccessArguments.None).ShouldBeTrue();

        // А менять его может игрок со своей (пусть и непринятой) заявкой.
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var playerAccess = AccessArgumentsFactory.Create(claim, mock.Player.UserId, mock.ProjectInfo);

        field.HasEditAccess(playerAccess).ShouldBeTrue();
    }

    [Fact]
    public void PlayerVisibleFieldCanBeEditableByPlayer()
    {
        var field = mock.CreateField(
            "Видно игроку, игрок может менять",
            ProjectFieldVisibility.PlayerAndMaster,
            canPlayerEdit: true,
            showOnUnApprovedClaims: true);

        // Непубличное поле видно только участникам: игроку — с утверждённой заявки.
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);
        var playerAccess = AccessArgumentsFactory.Create(claim, mock.Player.UserId, mock.ProjectInfo);

        field.HasViewAccess(playerAccess).ShouldBeTrue();
        field.HasEditAccess(playerAccess).ShouldBeTrue();
        field.HasViewAccess(AccessArguments.None).ShouldBeFalse();
    }

    [Fact]
    public void MasterOnlyFieldCantBeEditableByPlayer()
        => Should.Throw<InvalidOperationException>(() => mock.CreateField(
            "Только мастера, но игрок может менять",
            ProjectFieldVisibility.MasterOnly,
            canPlayerEdit: true));
}
