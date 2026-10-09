using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Schedules;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.Schedules;

/// <summary>
/// Тесты сетки расписания. Стали возможны после переезда <see cref="ScheduleBuilder"/> на
/// <see cref="CharacterInfo"/> (ADR013): раньше для них пришлось бы собирать EF-граф.
/// </summary>
public class ScheduleBuilderTest
{
    private const int TimeSlotFieldId = 10;
    private const int RoomFieldId = 20;
    private const int AuthorFieldId = 30;

    private static readonly DateTime Day = new(2026, 6, 1);

    private static DateTimeOffset AtProjectTime(DateTime time) => new(time, TimeSpan.FromHours(3));

    private static readonly IReadOnlyDictionary<UserIdentification, UserInfoHeader> NoAuthors
        = new Dictionary<UserIdentification, UserInfoHeader>();

    [Fact]
    public void ThrowsDomainExceptionWhenScheduleNotConfigured()
    {
        var projectInfo = MakeProject();

        var exception = Should.Throw<ScheduleNotEnabledException>(
            () => new ScheduleBuilder([], projectInfo, NoAuthors));

        exception.ProjectId.ShouldBe(ProjectId);
    }

    [Fact]
    public void PutsItemIntoSelectedSlot()
    {
        var projectInfo = MakeScheduleProject();
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1]);

        var result = new ScheduleBuilder([character], projectInfo, NoAuthors).Build();

        result.NotScheduled.ShouldBeEmpty();
        result.Conflicted.ShouldBeEmpty();
        result.Slots[0][0]!.Id.ShouldBe(character.Id);
        result.Slots[0][1].ShouldBeNull();
        result.Slots[1][0].ShouldBeNull();

        var placed = result.AllItems.ShouldHaveSingleItem();
        placed.StartTime.ShouldBe(AtProjectTime(Day.AddHours(10)));
        placed.EndTime.ShouldBe(AtProjectTime(Day.AddHours(11)));
        placed.Rooms.ShouldHaveSingleItem().Name.ShouldBe("Комната 1");
    }

    [Fact]
    public void SpansSeveralTimeSlotsAndRooms()
    {
        var projectInfo = MakeScheduleProject();
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1, 2], rooms: [1, 2]);

        var result = new ScheduleBuilder([character], projectInfo, NoAuthors).Build();

        result.NotScheduled.ShouldBeEmpty();
        result.Conflicted.ShouldBeEmpty();
        result.Slots.SelectMany(row => row).ShouldAllBe(item => item != null);

        var placed = result.AllItems.ShouldHaveSingleItem();
        placed.StartTime.ShouldBe(AtProjectTime(Day.AddHours(10)));
        placed.EndTime.ShouldBe(AtProjectTime(Day.AddHours(12)));
        placed.Rooms.Count.ShouldBe(2);
    }

    [Fact]
    public void ReportsConflictWhenTwoItemsShareSlot()
    {
        var projectInfo = MakeScheduleProject();
        var first = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1]);
        var second = MakeProgramItemCharacter(projectInfo, 2, timeSlots: [1], rooms: [1]);

        var result = new ScheduleBuilder([first, second], projectInfo, NoAuthors).Build();

        result.Conflicted.Select(item => item.Id).ShouldBe([first.Id, second.Id], ignoreOrder: true);
        // Победил тот, кто встал в слот первым — второй в сетку не попадает.
        result.Slots[0][0]!.Id.ShouldBe(first.Id);
        // Но в список всех пунктов программы попадают оба: у конфликтующего тоже есть время и место.
        result.AllItems.Count.ShouldBe(2);
    }

    [Fact]
    public void ReportsNotScheduledWhenNoSlotSelected()
    {
        var projectInfo = MakeScheduleProject();
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [], rooms: []);

        var result = new ScheduleBuilder([character], projectInfo, NoAuthors).Build();

        result.NotScheduled.ShouldHaveSingleItem().Id.ShouldBe(character.Id);
        result.AllItems.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsNotScheduledWhenSelectedRoomWasDeleted()
    {
        // Вариант остался в поле (иначе значение персонажа на него бы не ссылалось), но выключен,
        // поэтому колонки в сетке для него нет.
        var projectInfo = MakeScheduleProject(deletedRoomId: 2);
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1, 2]);

        var result = new ScheduleBuilder([character], projectInfo, NoAuthors).Build();

        result.NotScheduled.ShouldHaveSingleItem().Id.ShouldBe(character.Id);
        // Оставшаяся часть расстановки при этом не теряется.
        result.Slots[0][0]!.Id.ShouldBe(character.Id);
    }

    [Fact]
    public void IgnoresCharacterSlots()
    {
        var projectInfo = MakeScheduleProject();
        var slot = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1],
            characterTypeInfo: CharacterTypeInfo.DefaultSlot("Слот"));

        var result = new ScheduleBuilder([slot], projectInfo, NoAuthors).Build();

        result.AllItems.ShouldBeEmpty();
        result.NotScheduled.ShouldBeEmpty();
        result.Slots.SelectMany(row => row).ShouldAllBe(item => item == null);
    }

    [Fact]
    public void TakesAuthorFromApprovedClaim()
    {
        var projectInfo = MakeScheduleProject();
        var claim = MakeApprovedClaim(projectInfo);
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1], approvedClaim: claim);

        var result = new ScheduleBuilder([character], projectInfo, NoAuthors).Build();

        var item = result.AllItems.ShouldHaveSingleItem().ProgramItem;
        item.Authors.ShouldHaveSingleItem().UserId.ShouldBe(claim.PlayerId);
        item.ShowAuthors.ShouldBeTrue();
    }

    [Fact]
    public void HidesAuthorsWhenPlayerIsHidden()
    {
        var projectInfo = MakeScheduleProject();
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1],
            approvedClaim: MakeApprovedClaim(projectInfo),
            hidePlayerForCharacter: true);

        var result = new ScheduleBuilder([character], projectInfo, NoAuthors).Build();

        var item = result.AllItems.ShouldHaveSingleItem().ProgramItem;
        item.Authors.ShouldHaveSingleItem();
        item.ShowAuthors.ShouldBeFalse();
    }

    [Fact]
    public void TakesAuthorsFromAuthorFieldInsteadOfPlayer()
    {
        var projectInfo = MakeScheduleProject(authorFieldVisibility: ProjectFieldVisibility.Public);
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1],
            approvedClaim: MakeApprovedClaim(projectInfo),
            authorFieldValue: "300,301");

        var result = new ScheduleBuilder([character], projectInfo, MakeAuthorUsers(300, 301)).Build();

        var item = result.AllItems.ShouldHaveSingleItem().ProgramItem;
        item.Authors.Select(author => author.UserId.Value).ShouldBe([300, 301]);
        item.ShowAuthors.ShouldBeTrue();
    }

    [Fact]
    public void FallsBackToPlayerWhenAuthorFieldIsEmpty()
    {
        var projectInfo = MakeScheduleProject(authorFieldVisibility: ProjectFieldVisibility.Public);
        var claim = MakeApprovedClaim(projectInfo);
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1], approvedClaim: claim);

        var result = new ScheduleBuilder([character], projectInfo, NoAuthors).Build();

        var item = result.AllItems.ShouldHaveSingleItem().ProgramItem;
        item.Authors.ShouldHaveSingleItem().UserId.ShouldBe(claim.PlayerId);
    }

    /// <summary>
    /// Ведущий назван в поле явно, поэтому настройка персонажа «скрыть игрока» его не скрывает —
    /// видимость решает само поле (ADR017 §6).
    /// </summary>
    [Fact]
    public void ShowsAuthorsFromPublicFieldEvenWhenPlayerIsHidden()
    {
        var projectInfo = MakeScheduleProject(authorFieldVisibility: ProjectFieldVisibility.Public);
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1],
            hidePlayerForCharacter: true,
            authorFieldValue: "300");

        var result = new ScheduleBuilder([character], projectInfo, MakeAuthorUsers(300)).Build();

        result.AllItems.ShouldHaveSingleItem().ProgramItem.ShowAuthors.ShouldBeTrue();
    }

    [Fact]
    public void HidesAuthorsFromNonMastersWhenAuthorFieldIsNotPublic()
    {
        var projectInfo = MakeScheduleProject(authorFieldVisibility: ProjectFieldVisibility.MasterOnly);
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1],
            authorFieldValue: "300");

        var result = new ScheduleBuilder([character], projectInfo, MakeAuthorUsers(300)).Build();

        var item = result.AllItems.ShouldHaveSingleItem().ProgramItem;
        item.Authors.ShouldHaveSingleItem();
        item.ShowAuthors.ShouldBeFalse();
    }

    [Fact]
    public void SkipsAuthorThatWasDeleted()
    {
        var projectInfo = MakeScheduleProject(authorFieldVisibility: ProjectFieldVisibility.Public);
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1],
            approvedClaim: MakeApprovedClaim(projectInfo),
            authorFieldValue: "300,301");

        // Пользователя 301 в базе не нашлось — в словарь резолва он не попал.
        var result = new ScheduleBuilder([character], projectInfo, MakeAuthorUsers(300)).Build();

        var item = result.AllItems.ShouldHaveSingleItem().ProgramItem;
        // Ровно один автор: на игрока при непустом поле мы не откатываемся.
        item.Authors.ShouldHaveSingleItem().UserId.Value.ShouldBe(300);
    }

    [Fact]
    public void CollectAuthorUserIdsDeduplicatesAcrossCharacters()
    {
        var projectInfo = MakeScheduleProject(authorFieldVisibility: ProjectFieldVisibility.Public);
        var first = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1], authorFieldValue: "300,301");
        var second = MakeProgramItemCharacter(projectInfo, 2, timeSlots: [2], rooms: [1], authorFieldValue: "301");

        ProgramItem.CollectAuthorUserIds([first, second])
            .Select(id => id.Value)
            .ShouldBe([300, 301], ignoreOrder: true);
    }

    [Fact]
    public void CollectAuthorUserIdsIsEmptyWithoutAuthorField()
    {
        var projectInfo = MakeScheduleProject();
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1],
            approvedClaim: MakeApprovedClaim(projectInfo));

        ProgramItem.CollectAuthorUserIds([character]).ShouldBeEmpty();
    }

    [Fact]
    public void RejectsCharacterBoundToAnotherProjectInfoInstance()
    {
        var projectInfo = MakeScheduleProject();
        var otherProjectInfo = MakeScheduleProject();
        var alien = MakeProgramItemCharacter(otherProjectInfo, 1, timeSlots: [1], rooms: [1]);

        var ex = Should.Throw<ArgumentException>(() => new ScheduleBuilder([alien], projectInfo, NoAuthors));

        ex.ParamName.ShouldBe("characters");
    }

    [Fact]
    public void ThrowsWhenScheduleIsNotConfigured()
    {
        var projectInfo = MakeProject(MakeField(1));

        _ = Should.Throw<Exception>(() => new ScheduleBuilder([], projectInfo, NoAuthors));
    }

    #region Фикстуры

    /// <summary>
    /// Проект с двумя временными слотами (10:00 и 11:00, по часу) и двумя комнатами.
    /// </summary>
    /// <param name="deletedRoomId">Номер комнаты, которую надо пометить удалённой.</param>
    /// <param name="authorFieldVisibility">
    /// Если задана — в проекте есть ещё и поле «ведущий мероприятия» (#4512) с такой видимостью.
    /// </param>
    private static ProjectInfo MakeScheduleProject(
        int? deletedRoomId = null,
        ProjectFieldVisibility? authorFieldVisibility = null)
        => Build(
            fields:
            [
                .. authorFieldVisibility is { } visibility
                    ? new[] { MakeField(AuthorFieldId, ProjectFieldType.ScheduleAuthorField, visibility) }
                    : [],
                MakeField(
                    TimeSlotFieldId,
                    ProjectFieldType.ScheduleTimeSlotField,
                    variants:
                    [
                        MakeTimeSlotVariant(1, Day.AddHours(10)),
                        MakeTimeSlotVariant(2, Day.AddHours(11)),
                    ]),
                MakeField(
                    RoomFieldId,
                    ProjectFieldType.ScheduleRoomField,
                    variants:
                    [
                        MakeRoomVariant(1, isActive: deletedRoomId != 1),
                        MakeRoomVariant(2, isActive: deletedRoomId != 2),
                    ]),
            ],
            scheduleEnabled: true);

    private static TimeSlotFieldVariant MakeTimeSlotVariant(int variantId, DateTime startTime)
        => new(
            new ProjectFieldVariantIdentification(new ProjectFieldIdentification(ProjectId, TimeSlotFieldId), variantId),
            $"Слот {variantId}",
            Price: 0,
            IsPlayerSelectable: true,
            IsActive: true,
            CharacterGroupId: null,
            Description: null,
            MasterDescription: null,
            new TimeSlotOptions(startTime, TimeSlotInMinutes: 60).ToJson(),
            wasEverUsed: true,
            parentFieldName: "Слот",
            projectTimeZone: ProjectTimeZone);

    private static ProjectFieldVariant MakeRoomVariant(int variantId, bool isActive)
        => new(
            new ProjectFieldVariantIdentification(new ProjectFieldIdentification(ProjectId, RoomFieldId), variantId),
            $"Комната {variantId}",
            Price: 0,
            IsPlayerSelectable: true,
            IsActive: isActive,
            CharacterGroupId: null,
            Description: null,
            MasterDescription: null,
            ProgrammaticValue: null,
            WasEverUsed: true,
            ParentFieldName: "Комната");

    private static CharacterInfo MakeProgramItemCharacter(
        ProjectInfo projectInfo,
        int characterId,
        int[] timeSlots,
        int[] rooms,
        CharacterTypeInfo? characterTypeInfo = null,
        CharacterClaimInfo? approvedClaim = null,
        bool hidePlayerForCharacter = false,
        string? authorFieldValue = null)
        => new(
            new CharacterIdentification(projectInfo.ProjectId, characterId),
            projectInfo,
            $"Пункт программы {characterId}",
            characterTypeInfo ?? CharacterTypeInfo.Default(),
            hidePlayerForCharacter,
            isActive: true,
            inGame: false,
            autoCreated: false,
            new MarkdownString($"Описание {characterId}"),
            originalCharacterSlotId: null,
            directGroupIds: [],
            new FieldLayerContainer(projectInfo, MakeFieldValues(projectInfo, timeSlots, rooms, authorFieldValue)),
            plotElementOrderData: null,
            claims: approvedClaim is null ? [] : [approvedClaim],
            approvedClaim?.ClaimId,
            new DateTime(2026, 1, 1),
            DefaultMasterId,
            new DateTime(2026, 1, 1),
            DefaultMasterId);

    /// <remarks>
    /// Поле-ведущий попадает в слой только если оно есть в проекте: контейнер слоя падает на
    /// значении неизвестного поля.
    /// </remarks>
    private static Dictionary<int, string?> MakeFieldValues(
        ProjectInfo projectInfo,
        int[] timeSlots,
        int[] rooms,
        string? authorFieldValue)
    {
        var values = new Dictionary<int, string?>
        {
            [TimeSlotFieldId] = string.Join(",", timeSlots),
            [RoomFieldId] = string.Join(",", rooms),
        };
        if (projectInfo.ScheduleAuthorField is not null)
        {
            values[AuthorFieldId] = authorFieldValue;
        }
        return values;
    }

    private static Dictionary<UserIdentification, UserInfoHeader> MakeAuthorUsers(params int[] userIds)
        => userIds.ToDictionary(
            id => new UserIdentification(id),
            id => new UserInfoHeader(new UserIdentification(id), new UserDisplayName($"Ведущий {id}", null)));

    private static CharacterClaimInfo MakeApprovedClaim(ProjectInfo projectInfo)
        => new(
            new ClaimIdentification(ProjectId, 1),
            new UserInfoHeader(new UserIdentification(200), new UserDisplayName("Автор", null)),
            ClaimStatus.Approved,
            DenialStatus: null,
            DefaultMasterId,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 1),
            MasterAcceptedDate: new DateTime(2026, 1, 1),
            MasterDeclinedDate: null,
            PlayerDeclinedDate: null,
            CheckInDate: null,
            LastPlayerCommentAt: null,
            LastMasterCommentAt: null,
            LastVisibleMasterCommentAt: null,
            CommentDiscussionId: new CommentDiscussionId(1),
            Finance: new ClaimFinanceInfo(
                FixedFee: null,
                PreferentialFeeUser: false,
                FeePaid: 0,
                AccommodationFee: 0,
                OperationsRequireModeration: false),
            AccommodationTypeId: null,
            AccommodationGroupId: AccommodationGroupIdentification.From(new ClaimIdentification(ProjectId, 1)),
            PlayerAllowedSensitiveData: false,
            new FieldLayerContainer(projectInfo, new Dictionary<int, string?>()));

    #endregion
}
