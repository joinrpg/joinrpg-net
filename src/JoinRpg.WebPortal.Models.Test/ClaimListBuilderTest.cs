using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
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

        var legacy = claim.GetFields(Mock.ProjectInfo).ToDictionary(x => x.Field.Id, x => x.ExportString);
        result.FieldValues.ShouldBe(legacy);
        result.FieldValues[Mock.CharacterFieldInfo.Id].ShouldBe(approved ? "секрет персонажа" : "");

        var balance = claimInfo.ClaimInCharacter.CalculateBalance();
        result.TotalFee.ShouldBe(balance.TotalFee);
        result.FeeDue.ShouldBe(balance.FeeDue);
        result.FeePaid.ShouldBe(balance.FeePaid);
    }

    // Согласие игрока на защищённые данные открывает только то, что проект запросил:
    // проект, которому нужен один адрес, паспорт не получает.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ExportGivesOnlyRequestedSensitiveData(bool requestPassport, bool requestAddress)
    {
        Mock.Project.Details.RequirePassport = requestPassport ? MandatoryStatus.Recommended : MandatoryStatus.Optional;
        Mock.Project.Details.RequireRegistrationAddress = requestAddress ? MandatoryStatus.Recommended : MandatoryStatus.Optional;
        Mock.ReInitProjectInfo();
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = true;
        var player = Mock.PlayerInfo with { PassportData = "паспорт", RegistrationAddress = "адрес", BirthDate = new DateOnly(2000, 2, 1) };
        var claimInfo = new ClaimInfo(
            new ClaimInCharacter(Mock.GetCharacterInfo(Mock.Character), claim.GetId()),
            player);

        var result = ClaimListBuilder.BuildItemForExport(
            claim,
            new FakeCurrentUserAccessor(new UserIdentification(Mock.Master.UserId)),
            claimInfo,
            roomName: null);

        result.PassportData.ShouldBe(requestPassport ? "паспорт" : null);
        result.RegistrationAddress.ShouldBe(requestAddress ? "адрес" : null);
        result.BirthDate.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExportGivesBirthDateOnlyWithConsent(bool consent)
    {
        DateOnly? expected = consent ? new DateOnly(2000, 2, 1) : null;
        Mock.Project.Details.RequireBirthDate = MandatoryStatus.Recommended;
        Mock.ReInitProjectInfo();
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = consent;
        var player = Mock.PlayerInfo with { PassportData = "паспорт", BirthDate = new DateOnly(2000, 2, 1) };
        var claimInfo = new ClaimInfo(
            new ClaimInCharacter(Mock.GetCharacterInfo(Mock.Character), claim.GetId()),
            player);

        var result = ClaimListBuilder.BuildItemForExport(
            claim,
            new FakeCurrentUserAccessor(new UserIdentification(Mock.Master.UserId)),
            claimInfo,
            roomName: null);

        result.BirthDate.ShouldBe(expected);
        result.PassportData.ShouldBeNull();
    }

    // Закрытый проект защищённых данных не получает, даже если согласие осталось:
    // и в выгрузке, и на странице заявки решение одно — ClaimInfo.SensitiveData.
    [Fact]
    public void ArchivedProjectGetsNoSensitiveData()
    {
        Mock.Project.Details.RequirePassport = MandatoryStatus.Recommended;
        Mock.Project.Details.RequireBirthDate = MandatoryStatus.Recommended;
        Mock.Project.Active = false;
        Mock.Project.IsAcceptingClaims = false;
        Mock.ReInitProjectInfo();
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = true;
        var player = Mock.PlayerInfo with { PassportData = "паспорт", BirthDate = new DateOnly(2000, 2, 1) };
        var claimInfo = new ClaimInfo(
            new ClaimInCharacter(Mock.GetCharacterInfo(Mock.Character), claim.GetId()),
            player);

        claimInfo.SensitiveData.ShouldBe(new ClaimSensitiveData(false, null, false, null, false, null));
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

    // Регрессия для #6015: CreateDate лежит в datetime без пояса, отметки комментариев —
    // DateTimeOffset. Неявное приведение DateTime к DateTimeOffset приклеивало локальное
    // смещение сервера, а результат брался как .DateTime — со сдвигом Offset комментария.
    // Здесь смещение +3: 12:00 по Москве = 09:00 UTC, и «обновлено» обязано показывать UTC-время.
    [Fact]
    public void GetLastCommentNormalizesCommentOffsetToUtc()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.CreateDate = new DateTime(2026, 1, 1, 20, 0, 0, DateTimeKind.Unspecified);
        claim.LastPlayerCommentAt = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.FromHours(3));

        var result = ClaimListBuilder.GetLastComment(claim, AccessArguments.None);

        result.At.ShouldBe(new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Unspecified));
    }

    // Тоже #6015: комментарий игрока на 2 часа РАНЬШЕ подачи (по UTC). На сервере со смещением
    // +3 старое сравнение считало дату подачи на 3 часа позже, чем она есть, и комментарий
    // ошибочно побеждал — «кто обновил» показывал игрока вместо подачи.
    [Fact]
    public void GetLastCommentDoesNotLetEarlierCommentBeatSubmission()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.CreateDate = new DateTime(2026, 1, 2, 3, 0, 0, DateTimeKind.Unspecified);
        claim.LastVisibleMasterCommentAt = new DateTimeOffset(2026, 1, 2, 1, 0, 0, TimeSpan.Zero);
        claim.LastVisibleMasterCommentBy = Mock.Master;

        var result = ClaimListBuilder.GetLastComment(claim, AccessArguments.None);

        result.At.ShouldBe(claim.CreateDate);
        // «Кто обновил» при этом — игрок (подача), а не рано прокомментировавший мастер.
        result.By.UserId.Value.ShouldBe(Mock.Player.UserId);
    }

    // #6015 для агрегата (ADR013): та же нормализация в перегрузке, на которой считает
    // колонку «обновлено» UnifiedGrid (ADR027).
    [Fact]
    public void GetLastCommentTimeOnAggregateNormalizesCommentOffsetToUtc()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.CreateDate = new DateTime(2026, 1, 1, 20, 0, 0, DateTimeKind.Unspecified);
        claim.LastPlayerCommentAt = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.FromHours(3));

        var aggregateClaim = Mock.GetCharacterInfo(Mock.Character).Claims
            .ShouldHaveSingleItem();

        ClaimListBuilder.GetLastCommentTime(aggregateClaim, AccessArguments.None)
            .ShouldBe(new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Unspecified));
    }

    private sealed class NoProblemsValidator : IClaimProblemValidator
    {
        public IEnumerable<ClaimProblem> Validate(ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint) => [];
        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context) => [];
        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields) => [];
    }
}
