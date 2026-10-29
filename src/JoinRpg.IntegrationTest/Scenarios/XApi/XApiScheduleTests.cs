using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

/// <summary>
/// Расписание на живой БД. До этого сценария путь построения сетки не исполнялся ни в одном
/// интеграционном тесте: смоук по <c>{projectId}/schedule</c> упирался в «расписание не настроено»,
/// потому что сид не создаёт полей времени и локации.
/// </summary>
/// <remarks>
/// Проект заводится типом «программа конвента» — он единственный создаёт поля расписания сам,
/// так что сценарий повторяет боевой путь мастера. Свой проект на каждый тест: варианты полей
/// меняют метаданные, а фикстура XApi общая на всю коллекцию.
/// </remarks>
[Collection("XApi")]
public class XApiScheduleTests(XApiMasterFixture fixture)
{
    // Проект по умолчанию в Europe/Moscow
    private static readonly DateTimeOffset FirstSlotStart =
        new(2026, 7, 10, 10, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public async Task ReturnsScheduledProgramItems()
    {
        var project = await SeedScheduleProjectAsync();

        var fencing = await AddProgramItemAsync(project, "Мастер-класс по фехтованию", timeSlots: [0], rooms: [0]);
        var herbs = await AddProgramItemAsync(project, "Лекция о травах", timeSlots: [1], rooms: [1]);

        var schedule = await fixture.MasterClient.GetScheduleAsync(project.ProjectId);

        schedule.Count.ShouldBe(2);

        var fencingItem = schedule.Single(item => item.ProgramItemId == fencing);
        fencingItem.Name.ShouldBe("Мастер-класс по фехтованию");
        fencingItem.ProjectId.ShouldBe(project.ProjectId);
        fencingItem.StartTime.ShouldBe(FirstSlotStart);
        fencingItem.EndTime.ShouldBe(FirstSlotStart.AddMinutes(60));
        fencingItem.Rooms.ShouldHaveSingleItem().Name.ShouldBe("Шатёр");
        fencingItem.Authors.ShouldBeEmpty();
        fencingItem.ProgramItemDetailsUri.AbsolutePath
            .ShouldBe($"/{project.ProjectId.Value}/character/{fencing}/details");

        var herbsItem = schedule.Single(item => item.ProgramItemId == herbs);
        herbsItem.StartTime.ShouldBe(FirstSlotStart.AddHours(1));
        herbsItem.Rooms.ShouldHaveSingleItem().Name.ShouldBe("Поляна");
    }

    [Fact]
    public async Task MergesSeveralTimeSlotsAndRoomsIntoOneItem()
    {
        var project = await SeedScheduleProjectAsync();
        var characterId = await AddProgramItemAsync(project, "Двухчасовой полигон", timeSlots: [0, 1], rooms: [0, 1]);

        var item = (await fixture.MasterClient.GetScheduleAsync(project.ProjectId))
            .Single(x => x.ProgramItemId == characterId);

        item.StartTime.ShouldBe(FirstSlotStart);
        item.EndTime.ShouldBe(FirstSlotStart.AddHours(2));
        item.Rooms.Select(room => room.Name).ShouldBe(["Шатёр", "Поляна"], ignoreOrder: true);
    }

    [Fact]
    public async Task RendersDescriptionInAllThreeFormats()
    {
        var project = await SeedScheduleProjectAsync();
        var characterId = await AddProgramItemAsync(
            project,
            "Пункт с описанием",
            timeSlots: [0],
            rooms: [0],
            description: "Берём **меч** и идём");

        var item = (await fixture.MasterClient.GetScheduleAsync(project.ProjectId))
            .Single(x => x.ProgramItemId == characterId);

        item.DescriptionMarkdown.ShouldBe("Берём **меч** и идём");
        item.DescriptionHtml.ShouldNotBeNull().ShouldContain("<strong>меч</strong>");
        var plainText = item.Description.ShouldNotBeNull();
        plainText.ShouldContain("меч");
        plainText.ShouldNotContain("**");
    }

    [Fact]
    public async Task SkipsCharactersWithoutTimeSlot()
    {
        var project = await SeedScheduleProjectAsync();
        var scheduled = await AddProgramItemAsync(project, "В расписании", timeSlots: [0], rooms: [0]);
        _ = await AddProgramItemAsync(project, "Без времени и места", timeSlots: [], rooms: []);

        var schedule = await fixture.MasterClient.GetScheduleAsync(project.ProjectId);

        schedule.ShouldHaveSingleItem().ProgramItemId.ShouldBe(scheduled);
    }

    [Fact]
    public async Task ReturnsBadRequestWhenScheduleIsNotConfigured()
    {
        // В larp-проекте полей расписания нет вовсе.
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);

        var status = await fixture.MasterClient.GetScheduleRawAsync(projectId);

        status.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Внешним клиентам API обещан 410 на несуществующий проект, а не общий 404 портала.
    /// </summary>
    [Fact]
    public async Task ReturnsGoneWhenProjectDoesNotExist()
    {
        var status = await fixture.MasterClient.GetScheduleRawAsync(int.MaxValue);

        status.ShouldBe(System.Net.HttpStatusCode.Gone);
    }

    /// <summary>
    /// Ведущий берётся из поля «Ведущий» — его заводит сам шаблон проекта (#4512). Утверждённой
    /// заявки у персонажа тут нет, так что до поля список ведущих был бы пуст.
    /// </summary>
    [Fact]
    public async Task TakesAuthorsFromAuthorField()
    {
        var project = await SeedScheduleProjectAsync();
        var characterId = await AddProgramItemAsync(project, "Лекция с ведущим", timeSlots: [0], rooms: [0],
            authors: [fixture.MasterUserId.Value]);

        var item = (await fixture.MasterClient.GetScheduleAsync(project.ProjectId))
            .Single(x => x.ProgramItemId == characterId);

        item.Authors.ShouldHaveSingleItem().UserId.ShouldBe(fixture.MasterUserId.Value);
    }

    /// <summary>
    /// Расписание программы конвента публично: эндпоинт не требует JWT, а доступ решается
    /// видимостью полей времени и локации — шаблон проекта заводит их публичными.
    /// </summary>
    [Fact]
    public async Task WithoutAuth_ReturnsPublicSchedule()
    {
        var project = await SeedScheduleProjectAsync();
        var characterId = await AddProgramItemAsync(project, "Открытая лекция", timeSlots: [0], rooms: [0]);

        var schedule = await fixture.AnonymousXApiClient.GetScheduleAsync(project.ProjectId);

        schedule.ShouldHaveSingleItem().ProgramItemId.ShouldBe(characterId);
    }

    [Fact]
    public async Task ScheduleWithLastModified_ReturnsItemsAndTime()
    {
        // Отметки в БД — UTC без признака Kind: окно «до/после» ловит отдачу местного времени
        var before = DateTimeOffset.UtcNow.AddSeconds(-2);
        var project = await SeedScheduleProjectAsync();
        var characterId = await AddProgramItemAsync(project, "Лекция", timeSlots: [0], rooms: [0]);
        var after = DateTimeOffset.UtcNow.AddSeconds(2);

        var schedule = await fixture.MasterClient.GetScheduleWithLastModifiedAsync(project.ProjectId);

        var item = schedule.Items.ShouldHaveSingleItem();
        item.ProgramItemId.ShouldBe(characterId);
        item.LastModifiedAt.ShouldBeInRange(before, after);
        // Единственный пункт: общее время — тот же максимум по нему и полям расписания
        schedule.LastModifiedAt.ShouldBe(item.LastModifiedAt);
    }

    /// <summary>
    /// Зал — вариант поля, у самого персонажа ничего не меняется. Но клиент должен узнать, что
    /// у пункта теперь другой набор залов, иначе он не перекачает расписание.
    /// </summary>
    [Fact]
    public async Task EditingRooms_MovesLastModifiedOfItems()
    {
        var project = await SeedScheduleProjectAsync();
        _ = await AddProgramItemAsync(project, "Лекция", timeSlots: [0], rooms: [0]);
        var before = (await fixture.MasterClient.GetScheduleWithLastModifiedAsync(project.ProjectId))
            .Items.ShouldHaveSingleItem().LastModifiedAt;

        _ = await fixture.CreateFieldVariant(fixture.MasterUserId, project.Fields.RoomFieldId, "Новый шатёр");

        var after = await fixture.MasterClient.GetScheduleWithLastModifiedAsync(project.ProjectId);
        after.Items.ShouldHaveSingleItem().LastModifiedAt.ShouldBeGreaterThan(before);
        after.LastModifiedAt.ShouldBeGreaterThan(before);
    }

    /// <summary>
    /// Удалённый пункт пропадает из ответа — а время изменения расписания должно сдвинуться,
    /// хотя остальные пункты не менялись.
    /// </summary>
    [Fact]
    public async Task DeletingItem_MovesScheduleLastModified()
    {
        var project = await SeedScheduleProjectAsync();
        var kept = await AddProgramItemAsync(project, "Остаётся", timeSlots: [0], rooms: [0]);
        var deleted = await AddProgramItemAsync(project, "Удаляется", timeSlots: [1], rooms: [1]);
        var before = await fixture.MasterClient.GetScheduleWithLastModifiedAsync(project.ProjectId);

        await fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            sp => sp.GetRequiredService<ICharacterService>().DeleteCharacter(
                new DeleteCharacterRequest(new CharacterIdentification(project.ProjectId, deleted))));

        var after = await fixture.MasterClient.GetScheduleWithLastModifiedAsync(project.ProjectId);
        after.Items.ShouldHaveSingleItem().ProgramItemId.ShouldBe(kept);
        after.LastModifiedAt.ShouldBeGreaterThan(before.LastModifiedAt);
    }

    /// <summary>
    /// Шаблоны сборщик сетки не получает вовсе — но пункт, переведённый в шаблон, из расписания
    /// пропал, и клиент должен об этом узнать.
    /// </summary>
    [Fact]
    public async Task TurningItemIntoTemplate_MovesScheduleLastModified()
    {
        var project = await SeedScheduleProjectAsync();
        var kept = await AddProgramItemAsync(project, "Остаётся", timeSlots: [0], rooms: [0]);
        var templated = await AddProgramItemAsync(project, "Станет шаблоном", timeSlots: [1], rooms: [1]);
        var before = await fixture.MasterClient.GetScheduleWithLastModifiedAsync(project.ProjectId);

        await fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            async sp =>
            {
                var character = await sp.GetRequiredService<ICharacterInfoRepository>()
                    .GetCharacterInfo(new CharacterIdentification(project.ProjectId, templated));
                await sp.GetRequiredService<ICharacterService>().EditCharacter(new EditCharacterRequest(
                    character.Id,
                    character.DirectGroupIds,
                    CharacterTypeInfo.DefaultSlot("Станет шаблоном"),
                    character.CharacterFields));
            });

        var after = await fixture.MasterClient.GetScheduleWithLastModifiedAsync(project.ProjectId);
        after.Items.ShouldHaveSingleItem().ProgramItemId.ShouldBe(kept);
        after.LastModifiedAt.ShouldBeGreaterThan(before.LastModifiedAt);
    }

    #region Сид

    /// <summary>
    /// Проект «программа конвента» с двумя слотами по часу (10:00 и 11:00) и двумя площадками.
    /// </summary>
    private async Task<ScheduleProject> SeedScheduleProjectAsync()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId, ProjectTypeDto.ConventionProgram);
        var fields = await GetScheduleFieldsAsync(projectId);

        var timeSlotVariants = new[]
        {
            await fixture.CreateFieldVariant(fixture.MasterUserId, fields.TimeSlotFieldId, "10:00",
                new TimeSlotOptions(FirstSlotStart.DateTime, TimeSlotInMinutes: 60)),
            await fixture.CreateFieldVariant(fixture.MasterUserId, fields.TimeSlotFieldId, "11:00",
                new TimeSlotOptions(FirstSlotStart.AddHours(1).DateTime, TimeSlotInMinutes: 60)),
        };

        var roomVariants = new[]
        {
            await fixture.CreateFieldVariant(fixture.MasterUserId, fields.RoomFieldId, "Шатёр"),
            await fixture.CreateFieldVariant(fixture.MasterUserId, fields.RoomFieldId, "Поляна"),
        };

        return new ScheduleProject(projectId, fields, timeSlotVariants, roomVariants);
    }

    /// <summary>
    /// Поля расписания, имени и описания — их создаёт сам шаблон проекта, поэтому id берём
    /// из метаданных, а не из своего сида.
    /// </summary>
    private Task<ScheduleFields> GetScheduleFieldsAsync(ProjectIdentification projectId)
        => fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return new ScheduleFields(
                    projectInfo.TimeSlotField?.Id
                        ?? throw new InvalidOperationException("В шаблоне проекта нет поля времени"),
                    projectInfo.RoomField?.Id
                        ?? throw new InvalidOperationException("В шаблоне проекта нет поля локации"),
                    projectInfo.ScheduleAuthorField?.Id
                        ?? throw new InvalidOperationException("В шаблоне проекта нет поля ведущего"),
                    projectInfo.CharacterNameField?.Id
                        ?? throw new InvalidOperationException("В шаблоне проекта нет поля имени"),
                    projectInfo.CharacterDescriptionField?.Id
                        ?? throw new InvalidOperationException("В шаблоне проекта нет поля описания"));
            });

    /// <summary>
    /// Персонаж-пункт программы, созданный целиком через x-game-api. Пустые списки слотов или
    /// площадок означают незаполненное поле.
    /// </summary>
    private async Task<int> AddProgramItemAsync(
        ScheduleProject project,
        string name,
        int[] timeSlots,
        int[] rooms,
        string? description = null,
        int[]? authors = null)
    {
        var fieldValues = new Dictionary<int, JsonElement>
        {
            [project.Fields.NameFieldId.ProjectFieldId] = JsonSerializer.SerializeToElement(name),
        };

        if (description is not null)
        {
            fieldValues[project.Fields.DescriptionFieldId.ProjectFieldId] =
                JsonSerializer.SerializeToElement(description);
        }
        if (timeSlots.Length > 0)
        {
            fieldValues[project.Fields.TimeSlotFieldId.ProjectFieldId] =
                JsonSerializer.SerializeToElement(timeSlots.Select(i => project.TimeSlotVariants[i]));
        }
        if (rooms.Length > 0)
        {
            fieldValues[project.Fields.RoomFieldId.ProjectFieldId] =
                JsonSerializer.SerializeToElement(rooms.Select(i => project.RoomVariants[i]));
        }
        if (authors is { Length: > 0 })
        {
            fieldValues[project.Fields.AuthorFieldId.ProjectFieldId] =
                JsonSerializer.SerializeToElement(authors);
        }

        var header = await fixture.MasterClient.CreateCharacterAsync(project.ProjectId, new CreateCharacterRequest
        {
            CharacterType = CharacterTypeApi.Player,
            FieldValues = fieldValues,
        });

        return header.CharacterId;
    }

    private sealed record ScheduleFields(
        ProjectFieldIdentification TimeSlotFieldId,
        ProjectFieldIdentification RoomFieldId,
        ProjectFieldIdentification AuthorFieldId,
        ProjectFieldIdentification NameFieldId,
        ProjectFieldIdentification DescriptionFieldId);

    private sealed record ScheduleProject(
        ProjectIdentification ProjectId,
        ScheduleFields Fields,
        IReadOnlyList<int> TimeSlotVariants,
        IReadOnlyList<int> RoomVariants);

    #endregion
}
