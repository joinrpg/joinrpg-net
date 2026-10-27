using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Drp.MasterExports;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Exporters;
using static JoinRpg.Services.Drp.Test.MasterExports.MasterExportTestHelpers;

namespace JoinRpg.Services.Drp.Test.MasterExports;

/// <summary>
/// Круговые тесты: настоящие экспортёры и Excel-бэкенд выгрузки → файл → <see cref="MasterExportParser"/>.
/// Ломаются, если выгрузка поменяет формат так, что разбор её больше не понимает.
/// </summary>
public class MasterExportRoundTripTest
{
    private readonly MockedProject mock = new();

    private readonly Character approvedCharacter;
    private readonly Character freeCharacter;
    private readonly CharacterGroup elves;
    private readonly CharacterGroup forest;
    private readonly Claim approvedClaim;
    private readonly Claim pendingClaim;

    /// <summary>
    /// Поля проекта, названные как системные колонки: разбор должен отличить их от системных по месту.
    /// «ВК» первым — сразу за колонками игрока у заявок, «Игрок» последним — сразу перед ними у персонажей.
    /// </summary>
    private static readonly string[] FieldHeaders =
        ["ВК", "Адрес", "Комната", "Статус", "Имя", "Группы", "Персонаж", "Биография", "Пустое поле", "Игрок"];

    public MasterExportRoundTripTest()
    {
        elves = mock.CreateCharacterGroup();
        elves.CharacterGroupName = "Эльфы";
        forest = mock.CreateCharacterGroup();
        forest.CharacterGroupName = "Лес";

        approvedCharacter = mock.Character;
        MockedProject.AddCharToGroup(approvedCharacter, elves);
        MockedProject.AddCharToGroup(approvedCharacter, forest);
        approvedClaim = mock.CreateApprovedClaim(approvedCharacter, mock.Player);

        freeCharacter = mock.CreateCharacter("Свободный");
        pendingClaim = mock.CreateClaim(freeCharacter, mock.Player);
        pendingClaim.CreateDate = new DateTime(2026, 9, 1, 10, 20, 30, DateTimeKind.Utc);

        // AddField пересобирает метаданные и вытесняет поля, которые мок кладёт только в ProjectInfo:
        // в проекте остаются ровно эти поля.
        foreach (var name in FieldHeaders)
        {
            _ = mock.AddField(f =>
            {
                f.FieldName = name;
                f.FieldType = name == "Биография" ? ProjectFieldType.Text : ProjectFieldType.String;
                f.FieldBoundTo = name == "Пустое поле" ? FieldBoundTo.Claim : FieldBoundTo.Character;
            });
        }
        MockedProject.AssignFieldValues(approvedCharacter, new FieldWithValue(Field("Биография"), "первая строка\nвторая строка"));
    }

    private ProjectFieldInfo Field(string name) => mock.ProjectInfo.SortedFields.Single(f => f.Name == name);

    private void Archive()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
    }

    private sealed class NoProblemsValidator : ICharacterProblemValidator
    {
        public IEnumerable<ClaimProblem> Validate(CharacterInfo character, ProblemSeverity minimalSeverity = ProblemSeverity.Hint) => [];
        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character) => [];
        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character, IEnumerable<ProjectFieldIdentification> fields) => [];
    }

    private ClaimInfo ClaimInfoOf(Claim claim)
        => new(new ClaimInCharacter(mock.GetCharacterInfo(claim.Character), claim.GetId()), mock.PlayerInfo);

    private ClaimWithPlayer ClaimHeaderOf(Claim claim) => new()
    {
        ClaimId = claim.GetId(),
        CharacterName = claim.Character.CharacterName,
        Player = mock.PlayerInfo.ToUserInfoHeader(),
        ExtraNicknames = null,
        ResponsibleMasterUserId = new UserIdentification(claim.ResponsibleMasterUserId),
        CharacterId = claim.Character.GetId(),
    };

    private byte[] ExportCharacters(params Character[] extraCharacters)
    {
        var approvedClaims = new Dictionary<CharacterIdentification, ClaimInfo>
        {
            [approvedCharacter.GetId()] = ClaimInfoOf(approvedClaim),
        };
        var items = new[] { approvedCharacter, freeCharacter }
            .Concat(extraCharacters)
            .Select(c => new CharacterListItemViewModel(
                mock.GetCharacterInfo(c),
                new UserIdentification(mock.Master.UserId),
                approvedClaims,
                mock.ProjectInfo,
                new NoProblemsValidator()))
            .ToList();
        return ExportXlsx(items, new CharacterListItemViewModelExporter(mock.ProjectInfo, new PortalLikeUriService()));
    }

    private IEnumerable<ClaimListItemForExportViewModel> ClaimItems(IReadOnlyDictionary<ClaimIdentification, string>? roomNames = null)
    {
        Claim[] claims = [approvedClaim, pendingClaim];
        return new ClaimListForExportViewModel(
            new FakeCurrentUserAccessor(new UserIdentification(mock.Master.UserId)),
            claims,
            claims.ToDictionary(c => c.GetId(), ClaimInfoOf),
            roomNames ?? new Dictionary<ClaimIdentification, string>()).Items;
    }

    private byte[] ExportClaims(IReadOnlyDictionary<ClaimIdentification, string>? roomNames = null)
        => ExportXlsx(ClaimItems(roomNames), new ClaimListItemViewModelExporter(new PortalLikeUriService(), mock.ProjectInfo));

    [Fact]
    public void CharactersRoundTrip()
    {
        var result = Parse(ExportCharacters());

        result.Kind.ShouldBe(MasterExportKind.Characters);
        result.ProjectId.ShouldBe(mock.Project.ProjectId);
        result.OtherHeaders.ShouldBe(FieldHeaders);
        result.Warnings.ShouldBeEmpty();
        // ClosedXML ставит книге дату создания при выгрузке — это и есть момент выгрузки.
        var suggested = result.SuggestedDate.ShouldNotBeNull();
        (DateTimeOffset.UtcNow - suggested).Duration().ShouldBeLessThan(TimeSpan.FromMinutes(5));

        result.Rows.Count.ShouldBe(2);
        var approved = result.Rows[0];
        approved.RowNumber.ShouldBe(2);
        approved.CharacterId.ShouldBe(approvedCharacter.CharacterId);
        approved.ClaimId.ShouldBeNull();
        approved.Name.ShouldBe(approvedCharacter.CharacterName);
        approved.Groups.ShouldBe(
        [
            new MasterExportGroupRef(elves.CharacterGroupId, "Эльфы"),
            new MasterExportGroupRef(forest.CharacterGroupId, "Лес"),
        ], ignoreOrder: true);
        approved.ResponsibleMasterName.ShouldBe("Master");
        approved.PlayerEmail.ShouldBe("player@example.com");
        approved.PlayerDisplayName.ShouldBe("Player");
        approved.Other("Биография").ShouldBe("первая строка\nвторая строка");
        approved.Other("Пустое поле").ShouldBe("");
        approved.Warnings.ShouldBeEmpty();

        var free = result.Rows[1];
        free.CharacterId.ShouldBe(freeCharacter.CharacterId);
        free.Name.ShouldBe("Свободный");
        free.Groups.ShouldBeEmpty();
        free.PlayerEmail.ShouldBeNull();
    }

    /// <summary>Тип персонажа переживает выгрузку через «Занят?» (#5645).</summary>
    [Fact]
    public void CharacterTypesRoundTrip()
    {
        var slot = mock.CreateSlot("Шаблон");
        var hotSlot = mock.CreateSlot("Горячий шаблон");
        hotSlot.IsHot = true;
        var npc = mock.CreateCharacter("NPC");
        npc.CharacterType = CharacterType.NonPlayer;
        var hotVacancy = mock.CreateCharacter("Горячая вакансия");
        hotVacancy.IsHot = true;

        var result = Parse(ExportCharacters(slot, hotSlot, npc, hotVacancy));

        result.Warnings.ShouldBeEmpty();
        result.Rows.Select(r => r.CharacterType).ShouldBe(
        [
            // Занят и обсуждается — персонажи игроков.
            MasterExportCharacterType.Player,
            MasterExportCharacterType.Player,
            new MasterExportCharacterType(CharacterType.Slot, IsHot: false),
            new MasterExportCharacterType(CharacterType.Slot, IsHot: true),
            new MasterExportCharacterType(CharacterType.NonPlayer, IsHot: false),
            new MasterExportCharacterType(CharacterType.Player, IsHot: true),
        ]);
    }

    /// <summary>
    /// Проект запросил только дату рождения: её колонка идёт сразу за блоком игрока, без пары паспорт+адрес.
    /// Парсер её не узнаёт — она первый кандидат в поля, а распознавание остальных колонок не ломается.
    /// </summary>
    [Fact]
    public void ClaimsWithOnlyBirthDateRoundTrip()
    {
        mock.Project.Details.RequireBirthDate = MandatoryStatus.Recommended;
        approvedClaim.PlayerAllowedSenstiveData = true;
        mock.ReInitProjectInfo();

        var bytes = ExportClaims();
        var headers = XlsxHeaders(bytes);
        headers.ShouldContain("Дата рождения игрока");
        headers.ShouldNotContain("Паспортные данные");

        var result = Parse(bytes);

        result.Kind.ShouldBe(MasterExportKind.Claims);
        result.OtherHeaders.ShouldBe(["Дата рождения игрока", .. FieldHeaders]);
        result.Rows[0].PlayerEmail.ShouldBe("player@example.com");
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ClaimsRoundTrip()
    {
        var result = Parse(ExportClaims());

        result.Kind.ShouldBe(MasterExportKind.Claims);
        result.ProjectId.ShouldBe(mock.Project.ProjectId);
        result.OtherHeaders.ShouldBe(FieldHeaders);

        result.Rows.Count.ShouldBe(2);
        var approved = result.Rows[0];
        approved.ClaimId.ShouldBe(approvedClaim.ClaimId);
        approved.CharacterId.ShouldBeNull();
        approved.Name.ShouldBe(approvedCharacter.CharacterName);
        approved.Status.ShouldBe(ClaimStatus.Approved);
        approved.TotalFee.ShouldBe(0);
        approved.FeePaid.ShouldBe(0);
        approved.ResponsibleMasterName.ShouldBe("Master");
        approved.PlayerEmail.ShouldBe("player@example.com");
        approved.PlayerDisplayName.ShouldBe("Player");
        approved.Other("Пустое поле").ShouldBe("");
        approved.Other("Биография").ShouldBe("первая строка\nвторая строка");
        approved.Warnings.ShouldBeEmpty();

        var pending = result.Rows[1];
        pending.ClaimId.ShouldBe(pendingClaim.ClaimId);
        pending.Status.ShouldBe(ClaimStatus.AddedByUser);
        pending.ClaimCreatedAt.ShouldBe(new DateTimeOffset(2026, 9, 1, 10, 20, 30, TimeSpan.Zero));
        _ = pending.ClaimUpdatedAt.ShouldNotBeNull();
    }

    /// <summary>
    /// Все модули, добавляющие колонки: льготный взнос, поселение, паспорт и адрес, дата рождения.
    /// Дата заезда выгружается всегда, но проверяется здесь же — заодно со всеми необязательными колонками.
    /// </summary>
    [Fact]
    public void ClaimsWithAllModulesRoundTrip()
    {
        mock.Project.Details.PreferentialFeeEnabled = true;
        mock.Project.Details.EnableAccommodation = true;
        mock.Project.Details.RequirePassport = MandatoryStatus.Required;
        mock.Project.Details.RequireBirthDate = MandatoryStatus.Required;
        var accommodationType = mock.CreateAccommodationType(name: "Домик");
        _ = mock.CreateAccommodationRequest(accommodationType, approvedClaim);
        approvedClaim.PlayerAllowedSenstiveData = true;
        mock.ReInitProjectInfo();

        var bytes = ExportClaims(new Dictionary<ClaimIdentification, string> { [approvedClaim.GetId()] = "Комната 5" });
        // Страж самого теста: модули действительно добавили свои колонки.
        var headers = XlsxHeaders(bytes);
        headers.ShouldContain("Дата заезда");
        headers.ShouldContain("Льготник");
        headers.ShouldContain("Тип поселения");
        headers.ShouldContain("Паспортные данные");
        headers.ShouldContain("Дата рождения игрока");
        headers.Count(h => h == "Комната").ShouldBe(2);

        var result = Parse(bytes);

        result.Kind.ShouldBe(MasterExportKind.Claims);
        result.OtherHeaders.ShouldBe(["Дата рождения игрока", .. FieldHeaders]); // дату парсер не узнаёт, см. MasterExportColumns
        result.Rows.Select(r => r.ClaimId).ShouldBe([approvedClaim.ClaimId, pendingClaim.ClaimId]);
        result.Rows[0].Status.ShouldBe(ClaimStatus.Approved);
        result.Rows[0].PlayerEmail.ShouldBe("player@example.com");
        result.Rows[0].Other("Комната").ShouldBe("");
    }

    [Fact]
    public void ArchivedCharactersRoundTrip()
    {
        Archive();
        var bytes = ExportCharacters();
        XlsxHeaders(bytes)[^3..].ShouldBe(["Пустое поле", "Игрок", "Игрок"]);

        var result = Parse(bytes);

        result.Kind.ShouldBe(MasterExportKind.Characters);
        result.OtherHeaders.ShouldBe(FieldHeaders);
        var approved = result.Rows[0];
        approved.CharacterId.ShouldBe(approvedCharacter.CharacterId);
        approved.PlayerDisplayName.ShouldBe("Player");
        approved.PlayerEmail.ShouldBeNull();
        approved.Other("Игрок").ShouldBe("");
    }

    [Fact]
    public void ArchivedClaimsRoundTrip()
    {
        Archive();
        var bytes = ExportClaims();
        var headers = XlsxHeaders(bytes);
        headers.ShouldNotContain("Игрок.Email");
        headers.ShouldContain("ВК");

        var result = Parse(bytes);

        result.Kind.ShouldBe(MasterExportKind.Claims);
        result.OtherHeaders.ShouldBe(FieldHeaders);
        var approved = result.Rows[0];
        approved.ClaimId.ShouldBe(approvedClaim.ClaimId);
        approved.PlayerDisplayName.ShouldBe("Player");
        approved.PlayerEmail.ShouldBeNull();
    }

    /// <summary>«Мои заявки» — те же строки и ссылки на заявки, но без колонок мастерской выгрузки.</summary>
    [Fact]
    public void MyClaimsExport_IsRejected()
    {
        var bytes = ExportXlsx(ClaimItems(), new MyClaimListItemViewModelExporter(new PortalLikeUriService()));

        var ex = Should.Throw<MasterExportFormatException>(() => Parse(bytes));
        ex.Error.ShouldBe(MasterExportFormatError.UnknownExportType);
    }

    /// <summary>Финансовая выгрузка — ссылки на заявки с пустым заголовком, но слева «Заявка», а не «Имя».</summary>
    [Fact]
    public void FinanceExport_IsRejected()
    {
        var paymentType = mock.CreateCashPaymentType();
        var operation = new FinanceOperation
        {
            ProjectId = mock.Project.ProjectId,
            Project = mock.Project,
            ClaimId = approvedClaim.ClaimId,
            Claim = approvedClaim,
            MoneyAmount = 1500,
            PaymentTypeId = paymentType.PaymentTypeId,
            PaymentType = paymentType,
            CommentId = 1,
            Comment = new Comment { Author = mock.Master },
            OperationDate = new DateTime(2026, 9, 1),
        };
        var items = new[] { new FinOperationListItemViewModel(operation, ClaimHeaderOf(approvedClaim), mock.MasterInfo.ToUserInfoHeader(), mock.PlayerInfo, new PortalLikeUriService(), mock.ProjectInfo, currentUser: null) };
        var bytes = ExportXlsx(items, new FinanceOperationExporter(new PortalLikeUriService(), mock.ProjectInfo));

        var ex = Should.Throw<MasterExportFormatException>(() => Parse(bytes));
        ex.Error.ShouldBe(MasterExportFormatError.UnknownExportType);
    }
}
