using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.WebPortal.Models.Test;

public class ClaimListBuilderTest
{
    private MockedProject Mock { get; } = new MockedProject();

    // Регрессия для #4670: GetLastComment раньше вызывал User.GetUserInfo(), который
    // обращается к user.Auth без null-conditional. В реальном приложении это оборачивалось
    // в лишний lazy load почти на каждую заявку в списке; в этом тесте (без EF-контекста)
    // непрогруженный Auth равен null и вызов падает с NullReferenceException.
    [Fact]
    public void GetLastCommentDoesNotTouchUnloadedNavigationProperties()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        var result = ClaimListBuilder.GetLastComment(claim, AccessArguments.None);

        result.By.UserId.Value.ShouldBe(Mock.Player.UserId);
    }

    // Регрессия для #5166: BuildItemForExport читал имя типа поселения через навигацию
    // AccommodationRequest.AccommodationType, которой нет в Include списка заявок, — выходила
    // ленивая загрузка ProjectAccommodationTypes на каждую заявку (28 на один запрос на проде).
    // Здесь навигация намеренно оставлена непрогруженной: имя должно приехать из метаданных.
    [Fact]
    public void ExportTakesAccommodationTypeNameFromProjectMetadata()
    {
        var accommodationType = Mock.CreateAccommodationType(name: "Домик");
        Mock.ReInitProjectInfo();
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var request = Mock.CreateAccommodationRequest(accommodationType, claim);
        // Так выглядит заявка, пришедшая из ClaimsRepositoryImpl: сама заявка на поселение
        // прогружена, тип комнаты — нет.
        request.AccommodationType = null!;

        var result = ClaimListBuilder.BuildItemForExport(
            claim,
            new FakeCurrentUserAccessor(new UserIdentification(Mock.Master.UserId)),
            Mock.ProjectInfo,
            Mock.PlayerInfo);

        result.AccomodationType.ShouldBe("Домик");
    }
}
