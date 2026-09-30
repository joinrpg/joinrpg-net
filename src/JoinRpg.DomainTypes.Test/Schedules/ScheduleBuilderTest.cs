using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
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

    private static readonly DateTimeOffset Day = new(2026, 6, 1, 0, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public void PutsItemIntoSelectedSlot()
    {
        var projectInfo = MakeScheduleProject();
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1]);

        var result = new ScheduleBuilder([character], projectInfo).Build();

        result.NotScheduled.ShouldBeEmpty();
        result.Conflicted.ShouldBeEmpty();
        result.Slots[0][0]!.Id.ShouldBe(character.Id);
        result.Slots[0][1].ShouldBeNull();
        result.Slots[1][0].ShouldBeNull();

        var placed = result.AllItems.ShouldHaveSingleItem();
        placed.StartTime.ShouldBe(Day.AddHours(10));
        placed.EndTime.ShouldBe(Day.AddHours(11));
        placed.Rooms.ShouldHaveSingleItem().Name.ShouldBe("Комната 1");
    }

    [Fact]
    public void SpansSeveralTimeSlotsAndRooms()
    {
        var projectInfo = MakeScheduleProject();
        var character = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1, 2], rooms: [1, 2]);

        var result = new ScheduleBuilder([character], projectInfo).Build();

        result.NotScheduled.ShouldBeEmpty();
        result.Conflicted.ShouldBeEmpty();
        result.Slots.SelectMany(row => row).ShouldAllBe(item => item != null);

        var placed = result.AllItems.ShouldHaveSingleItem();
        placed.StartTime.ShouldBe(Day.AddHours(10));
        placed.EndTime.ShouldBe(Day.AddHours(12));
        placed.Rooms.Count.ShouldBe(2);
    }

    [Fact]
    public void ReportsConflictWhenTwoItemsShareSlot()
    {
        var projectInfo = MakeScheduleProject();
        var first = MakeProgramItemCharacter(projectInfo, 1, timeSlots: [1], rooms: [1]);
        var second = MakeProgramItemCharacter(projectInfo, 2, timeSlots: [1], rooms: [1]);

        var result = new ScheduleBuilder([first, second], projectInfo).Build();

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

        var result = new ScheduleBuilder([character], projectInfo).Build();

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

        var result = new ScheduleBuilder([character], projectInfo).Build();

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

        var result = new ScheduleBuilder([slot], projectInfo).Build();

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

        var result = new ScheduleBuilder([character], projectInfo).Build();

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

        var result = new ScheduleBuilder([character], projectInfo).Build();

        var item = result.AllItems.ShouldHaveSingleItem().ProgramItem;
        item.Authors.ShouldHaveSingleItem();
        item.ShowAuthors.ShouldBeFalse();
    }

    [Fact]
    public void RejectsCharacterBoundToAnotherProjectInfoInstance()
    {
        var projectInfo = MakeScheduleProject();
        var otherProjectInfo = MakeScheduleProject();
        var alien = MakeProgramItemCharacter(otherProjectInfo, 1, timeSlots: [1], rooms: [1]);

        var ex = Should.Throw<ArgumentException>(() => new ScheduleBuilder([alien], projectInfo));

        ex.ParamName.ShouldBe("characters");
    }

    [Fact]
    public void ThrowsWhenScheduleIsNotConfigured()
    {
        var projectInfo = MakeProject(MakeField(1));

        _ = Should.Throw<Exception>(() => new ScheduleBuilder([], projectInfo));
    }

    #region Фикстуры

    /// <summary>
    /// Проект с двумя временными слотами (10:00 и 11:00, по часу) и двумя комнатами.
    /// </summary>
    /// <param name="deletedRoomId">Номер комнаты, которую надо пометить удалённой.</param>
    private static ProjectInfo MakeScheduleProject(int? deletedRoomId = null)
        => Build(
            fields:
            [
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

    private static TimeSlotFieldVariant MakeTimeSlotVariant(int variantId, DateTimeOffset startTime)
        => new(
            new ProjectFieldVariantIdentification(new ProjectFieldIdentification(ProjectId, TimeSlotFieldId), variantId),
            $"Слот {variantId}",
            Price: 0,
            IsPlayerSelectable: true,
            IsActive: true,
            CharacterGroupId: null,
            Description: null,
            MasterDescription: null,
            JsonSerializer.Serialize(new TimeSlotOptions { StartTime = startTime, TimeSlotInMinutes = 60 }),
            wasEverUsed: true,
            parentFieldName: "Слот");

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
        bool hidePlayerForCharacter = false)
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
            new FieldLayerContainer(projectInfo, new Dictionary<int, string?>
            {
                [TimeSlotFieldId] = string.Join(",", timeSlots),
                [RoomFieldId] = string.Join(",", rooms),
            }),
            claims: approvedClaim is null ? [] : [approvedClaim],
            approvedClaim?.ClaimId,
            new DateTime(2026, 1, 1),
            DefaultMasterId,
            new DateTime(2026, 1, 1),
            DefaultMasterId);

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
            CurrentFee: null,
            PreferentialFeeUser: false,
            FeePaid: 0,
            AccommodationFee: 0,
            new FieldLayerContainer(projectInfo, new Dictionary<int, string?>()));

    #endregion
}
