using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.Domain;using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
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
    // Здесь навигация намеренно оставлена непрогруженной: имя должно приехать из метаданных
    // по идентификатору типа из агрегата (ADR021).
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
        var claimInfo = new ClaimInfo(
            new ClaimInCharacter(Mock.GetCharacterInfo(Mock.Character), claim.GetId()),
            Mock.PlayerInfo);

        var result = ClaimListBuilder.BuildItemForExport(
            claim,
            new FakeCurrentUserAccessor(new UserIdentification(Mock.Master.UserId)),
            claimInfo,
            roomName: null);

        result.AccomodationType.ShouldBe("Домик");
    }

    // Комната берётся из планов поселения (ADR022), а не цепочкой AccommodationRequest.Accommodation:
    // навигации на комнату у заявки из списка нет, и выгрузка не должна в неё ходить.
    [Fact]
    public void ExportTakesRoomNameFromPlans_NotFromNavigation()
    {
        var accommodationType = Mock.CreateAccommodationType(name: "Домик");
        Mock.ReInitProjectInfo();
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        _ = Mock.CreateRoom(Mock.CreateAccommodationRequest(accommodationType, claim), "по навигации");
        var claimInfo = new ClaimInfo(
            new ClaimInCharacter(Mock.GetCharacterInfo(Mock.Character), claim.GetId()),
            Mock.PlayerInfo);

        var result = ClaimListBuilder.BuildItemForExport(
            claim,
            new FakeCurrentUserAccessor(new UserIdentification(Mock.Master.UserId)),
            claimInfo,
            roomName: "из плана");

        result.RoomName.ShouldBe("из плана");
    }

    // Поля и баланс в выгрузке берутся из агрегата (ADR021), а не из EF-заявки. Легаси-путь
    // (CustomFieldsExtensions.GetFields(Claim)) скрывает от неутверждённой заявки непубличные поля
    // персонажа; ClaimInCharacter.GetAllFields() обязан делать то же — и в полях, и во взносе.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExportFieldsMatchLegacyCalculation(bool approved)
    {
        MockedProject.AssignFieldValues(Mock.Character,
            new FieldWithValue(Mock.CharacterFieldInfo, "секрет персонажа"),
            new FieldWithValue(Mock.PublicFieldInfo, "публичное"));
        var claim = approved
            ? Mock.CreateApprovedClaim(Mock.Character, Mock.Player)
            : Mock.CreateClaim(Mock.Character, Mock.Player);
        var claimInfo = new ClaimInfo(
            new ClaimInCharacter(Mock.GetCharacterInfo(Mock.Character), claim.GetId()),
            Mock.PlayerInfo);

        var result = ClaimListBuilder.BuildItemForExport(
            claim,
            new FakeCurrentUserAccessor(new UserIdentification(Mock.Master.UserId)),
            claimInfo,
            roomName: null);

        var legacy = claim.GetFields(Mock.ProjectInfo).ToDictionary(x => x.Field.Id, x => x.DisplayString);
        result.FieldValues.ShouldBe(legacy);
        result.FieldValues[Mock.CharacterFieldInfo.Id].ShouldBe(approved ? "секрет персонажа" : "");

        var balance = claimInfo.ClaimInCharacter.CalculateBalance();
        result.TotalFee.ShouldBe(balance.TotalFee);
        result.FeeDue.ShouldBe(balance.FeeDue);
        result.FeePaid.ShouldBe(balance.FeePaid);
    }

    // EF-версия списка отдавала пустую строку, если у игрока нет ФИО, а доменный
    // UserFullName.FullName — null. Колонка не nullable, прежнее поведение сохраняем.
    [Fact]
    public void ListItemPlayerFullNameIsEmptyWhenPlayerHasNoFullName()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var player = Mock.PlayerInfo with { UserFullName = new(null, null, null, null) };
        var claimInfo = new ClaimInfo(
            new ClaimInCharacter(Mock.GetCharacterInfo(Mock.Character), claim.GetId()),
            player);

        var result = ClaimListBuilder.BuildItem(
            claim,
            new FakeCurrentUserAccessor(new UserIdentification(Mock.Master.UserId)),
            new NoProblemsValidator(),
            claimInfo,
            []);

        result.PlayerFullName.ShouldBe("");
    }

    private sealed class NoProblemsValidator : IClaimProblemValidator
    {
        public IEnumerable<ClaimProblem> Validate(ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint) => [];
        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context) => [];
        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields) => [];
    }
}
