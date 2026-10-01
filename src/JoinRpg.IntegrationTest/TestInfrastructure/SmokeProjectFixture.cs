using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Services.Interfaces.Subscribe;
using JoinRpg.Web.Models.CommonTypes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Проект, наполненный так, чтобы по нему открывалось как можно больше страниц приложения (#4956).
/// </summary>
/// <remarks>
/// Сид один на весь смоук: поднятие проекта со всей обвязкой — самая долгая часть, а страницы
/// только читают. Наполнение намеренно «многострочное» — по три персонажа, заявки от разных
/// игроков, комментарии: на проекте из одного персонажа N+1 не виден, он даёт одну ленивую
/// загрузку на запрос и в снапшот попадает единицей (см. разбор логов прода в #4956).
/// </remarks>
public sealed class SmokeProjectFixture : IAsyncLifetime
{
    private const string Password = "Password123!";
    private const int CharacterCount = 3;

    public JoinApplicationFactory Factory { get; } = new();

    /// <summary>
    /// Клиент под мастером-владельцем проекта. Редиректы намеренно не проходит: иначе 302 на
    /// страницу входа или на главную выглядел бы как честный 200.
    /// </summary>
    public HttpClient MasterClient { get; private set; } = null!;

    /// <summary>Значения параметров URL, выведенные из этого проекта.</summary>
    internal SmokeParameterValues Values { get; } = new();

    /// <summary>Проект сида — нужен сценариям, которые строят URL сами.</summary>
    public ProjectIdentification ProjectId { get; private set; } = null!;

    /// <summary>Единственный тип поселения сида, у него есть жильцы.</summary>
    public int RoomTypeId { get; private set; }

    /// <summary>Названия комнат, созданных у <see cref="RoomTypeId"/>.</summary>
    public IReadOnlyList<string> RoomNames => SeededRoomNames;

    /// <summary>Вместимость одной комнаты типа <see cref="RoomTypeId"/>.</summary>
    public int RoomCapacity => SeededRoomCapacity;

    /// <summary>
    /// Жильцы типа поселения: отображаемое имя и телефон игрока.
    /// </summary>
    /// <remarks>
    /// Заявка на проживание есть у всех заявок сида, в том числе у отклонённой. На странице типа
    /// поселения видны все, а в отчёт по расселению отклонённая не попадает
    /// (<c>ClaimStatusSpec.Active</c> исключает отклонённые статусы) — отсюда флаг.
    /// </remarks>
    public IReadOnlyList<SmokeResident> Residents { get; private set; } = [];

    public async Task InitializeAsync()
    {
        await ((IAsyncLifetime)Factory).InitializeAsync();

        var (ownerId, ownerEmail) = await CreateUserAsync();
        var (secondMasterId, _) = await CreateUserAsync();

        var projectId = await CreateProjectAsync(ownerId);
        await GrantMasterAccessAsync(ownerId, projectId, secondMasterId);
        await EnableProjectModulesAsync(ownerId, projectId);

        var seeded = await SeedProjectContentAsync(ownerId, projectId);
        var players = await SeedClaimsAsync(ownerId, projectId, seeded.Characters);
        var placedResidentCount = await SeedAccommodationRequestsAsync(
            ownerId, projectId, seeded, players.AllClaims);

        MasterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            Password,
            followsRedirects: false);

        // Вход должен был сработать: дальше весь смоук считает, что ходит под мастером, и без
        // этой проверки сотни редиректов на страницу входа выглядели бы как «страница не 200».
        var mastersOnlyPage = await MasterClient.GetAsync($"{projectId.Value}/plots");
        mastersOnlyPage.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            "Мастерская страница не открылась — значит клиент смоука не аутентифицирован");

        RegisterValues(projectId, ownerId, secondMasterId, seeded, players, placedResidentCount);
    }

    public async Task DisposeAsync()
    {
        MasterClient?.Dispose();
        await ((IAsyncLifetime)Factory).DisposeAsync();
    }

    private void RegisterValues(
        ProjectIdentification projectId,
        UserIdentification ownerId,
        UserIdentification secondMasterId,
        SeededContent seeded,
        SeededClaims players,
        int placedResidentCount)
    {
        var mainCharacter = seeded.Characters[0];

        ProjectId = projectId;
        RoomTypeId = seeded.RoomTypeId;

        // Заявки на проживание создавались в том же порядке, что и игроки, а расселялись первые
        // placedResidentCount из них — отсюда и флаг расселённости.
        Residents =
        [
            .. players.PlayerIds.Select((id, index) => new SmokeResident(
                DisplayNameFor(id),
                PhoneNumberFor(id),
                // Отклонённая заявка в отчёт по расселению не попадает.
                ClaimIsActive: id != players.DeclinedPlayerId,
                IsPlaced: index < placedResidentCount)),
        ];

        _ = Values
            .Add("projectId", projectId.Value, projectId)
            .Add("characterId", mainCharacter.CharacterId, mainCharacter)
            .Add("characterGroupId", seeded.GroupId.CharacterGroupId, seeded.GroupId)
            // Перемещение группы вверх/вниз смотрит на родителя и на корень отдельными параметрами.
            .Add("parentCharacterGroupId", seeded.RootGroupId.CharacterGroupId, seeded.RootGroupId)
            .Add("currentRootGroupId", seeded.RootGroupId.CharacterGroupId, seeded.RootGroupId)
            .Add("groupId", seeded.GroupId.CharacterGroupId, seeded.GroupId)
            .Add("claimId", players.ApprovedClaimId.ClaimId, players.ApprovedClaimId)
            .Add("userId", ownerId.Value, ownerId)
            .Add("masterId", secondMasterId.Value, secondMasterId)
            .Add("responsibleMasterId", secondMasterId.Value, secondMasterId)
            .Add("projectFieldId", seeded.DropdownFieldId.ProjectFieldId, seeded.DropdownFieldId)
            .Add("valueId", seeded.DropdownVariantId)
            .Add("plotFolderId", seeded.Plot.PlotFolderId.PlotFolderId, seeded.Plot.PlotFolderId)
            .Add("plotElementId", seeded.Plot.ElementIds[0].PlotElementId)
            .Add("elementId", seeded.Plot.ElementIds[0].PlotElementId, seeded.Plot.ElementIds[0])
            .Add("copyFrom", seeded.Plot.ElementIds[0].PlotElementId, seeded.Plot.ElementIds[0])
            .Add("version", 1)
            .Add("roomTypeId", seeded.RoomTypeId)
            .Add("forumThreadId", seeded.ForumThreadId.ThreadId, seeded.ForumThreadId)
            .Add("commentId", players.CommentId)
            .Add("commentDiscussionId", players.CommentDiscussionId)
            .Add("financeOperationId", players.FinanceOperationId)
            .Add("paymentTypeId", seeded.PaymentTypeId.PaymentTypeId, seeded.PaymentTypeId)
            .Add("subscriptionId", seeded.SubscriptionId)
            // Списки ролей: id — в маршруте страницы, targetId/projectRolesListId — в query у webapi.
            .Add("id", seeded.RolesListId.ProjectRolesListId, seeded.RolesListId)
            .Add("targetId", seeded.RolesListId.ProjectRolesListId, seeded.RolesListId)
            .Add("projectRolesListId", seeded.RolesListId.ProjectRolesListId, seeded.RolesListId)
            // Печать и массовая рассылка принимают пачку id одним сжатым параметром.
            .Add(
                "characterIds",
                new CompressedIntList([.. seeded.Characters.Select(c => c.CharacterId)]))
            .Add("claimIds", new CompressedIntList([players.ApprovedClaimId.ClaimId]));
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = Factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider, password: Password);
    }

    private async Task<ProjectIdentification> CreateProjectAsync(UserIdentification ownerId)
    {
        using var scope = Factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateProjectAsync(
            scope.ServiceProvider, ownerId, "Смоук по всем страницам");
    }

    private Task GrantMasterAccessAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId,
        UserIdentification secondMasterId)
        => Factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = secondMasterId,
                Permissions = Enum.GetValues<Permission>().Where(p => p != Permission.None).ToArray(),
            }));

    /// <summary>
    /// Включает модули, без которых их страницы отдают не 200: приём заявок, поселение, чек-ин.
    /// </summary>
    private Task EnableProjectModulesAsync(UserIdentification ownerId, ProjectIdentification projectId)
        => Factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);

            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetAccommodationSettings(projectId, enableAccommodation: true);
            await projectService.SetCheckInSettings(
                projectId,
                checkInProgress: true,
                enableCheckInModule: true,
                modelAllowSecondRoles: true);
        });

    private Task<SeededContent> SeedProjectContentAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId)
        => Factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var rootGroupId = projectInfo.GroupTree.RootGroupId;
            var nameFieldId = (projectInfo.CharacterNameField
                    ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                .Id.ProjectFieldId;

            var groupId = await sp.GetRequiredService<ICharacterGroupService>().AddCharacterGroup(
                projectId,
                "Смоук-группа",
                isPublic: true,
                parentCharacterGroupIds: [rootGroupId],
                description: "Группа для смоука");

            var characterService = sp.GetRequiredService<ICharacterService>();
            var characters = new List<CharacterIdentification>(CharacterCount);
            for (var i = 1; i <= CharacterCount; i++)
            {
                characters.Add(await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [groupId],
                    new CharacterTypeInfo(
                        CharacterType.Player,
                        IsHot: i == 1,
                        SlotLimit: null,
                        SlotName: null,
                        CharacterVisibility.Public),
                    FieldValues: new FieldLayerContainer(
                        projectInfo,
                        new Dictionary<int, string?> { [nameFieldId] = $"Смоук-персонаж {i}" }))));
            }

            // Скрытая группа и скрытый персонаж: проверка видимости
            // (CharacterViewModelBuilder.IsVisible, в поиске — WorldObjectProviderBase) выглядит как
            // IsPublic || PublishPlot || HasMasterAccess(...), и на публичном
            // объекте останавливается на первом операнде, не касаясь проекта. Пока в сиде всё
            // было публичным, страницы со списками объектов мерились в ноль, хотя на проде
            // догружали Projects/ProjectDetails/ProjectAcls на каждую строку (#4989, #4991).
            var hiddenGroupId = await sp.GetRequiredService<ICharacterGroupService>().AddCharacterGroup(
                projectId,
                "Скрытая смоук-группа",
                isPublic: false,
                parentCharacterGroupIds: [groupId],
                description: "Непубличная группа для смоука");
            _ = await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [hiddenGroupId],
                new CharacterTypeInfo(
                    CharacterType.Player,
                    IsHot: false,
                    SlotLimit: null,
                    SlotName: null,
                    CharacterVisibility.Private),
                FieldValues: new FieldLayerContainer(
                    projectInfo,
                    new Dictionary<int, string?> { [nameFieldId] = "Скрытый смоук-персонаж" })));

            var (dropdownFieldId, dropdownVariantId) = await SeedDropdownFieldAsync(sp, projectId);

            // Значение «смоук-поля» у первого персонажа: страница ByAssignedField показывает заявки
            // с заполненным полем, и без единой такой строки список пуст — маршрут мерялся бы
            // в ноль и в снапшот не попал (#5135).
            await characterService.SetFields(
                characters[0],
                new FieldLayerContainer(
                    projectInfo,
                    new Dictionary<int, string?> { [dropdownFieldId.ProjectFieldId] = dropdownVariantId.ToString() }));

            // Сюжет наполняется «в ширину»: три вводных, у каждой в таргетах все персонажи сида и
            // две группы. На папке из двух вводных с одним таргетом N+1 по
            // PlotElementCharacters/PlotElementCharacterGroups не отличить от одиночного запроса,
            // поэтому страницы сюжетов и мерились в ноль при живом долге на проде (#4963, #4988).
            var plot = await TestPlotHelpers.SeedPlotFolderAsync(
                sp,
                projectId,
                elementCount: 3,
                extraTargetChars: characters,
                extraTargetGroups: [groupId]);
            _ = await TestPlotHelpers.SeedHandoutAsync(sp, plot.PlotFolderId, characters[0]);

            // Вторая папка: списки сюжетов (plots, plots/FlatList) на одной строке тоже не
            // показывают N+1 по папкам (#4965).
            _ = await TestPlotHelpers.SeedPlotFolderAsync(
                sp,
                projectId,
                elementCount: 2,
                extraTargetChars: characters,
                extraTargetGroups: [groupId]);

            var forumThreadId = await sp.GetRequiredService<IForumService>().CreateThread(
                groupId,
                "Смоук-обсуждение",
                "Первый комментарий в теме",
                hideFromUser: false,
                emailEverybody: false);

            var roomTypeId = await SeedRoomTypeAsync(sp, projectId);

            var rolesList = await sp.GetRequiredService<IProjectRolesListService>().CreateAsync(
                new JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesList(
                    projectId,
                    "Смоук-список ролей",
                    [dropdownFieldId],
                    RolesGridGroupsViewMode.None));

            var subscriptionId = await SeedSubscriptionAsync(sp, projectId, groupId, ownerId);
            var paymentTypeId = await SeedFinancesAsync(sp, projectId, ownerId);

            return new SeededContent(
                rootGroupId,
                groupId,
                characters,
                dropdownFieldId,
                dropdownVariantId,
                plot,
                forumThreadId,
                roomTypeId,
                rolesList.ProjectRolesListId
                    ?? throw new InvalidOperationException("Список ролей создан без id"),
                subscriptionId,
                paymentTypeId);
        });

    private static async Task<(ProjectFieldIdentification FieldId, int VariantId)> SeedDropdownFieldAsync(
        IServiceProvider sp,
        ProjectIdentification projectId)
    {
        var fieldSetupService = sp.GetRequiredService<IFieldSetupService>();
        var fieldId = await fieldSetupService.AddField(new CreateFieldRequest(
            projectId,
            ProjectFieldType.Dropdown,
            "Смоук-поле",
            fieldHint: "",
            canPlayerEdit: true,
            canPlayerView: true,
            isPublic: true,
            FieldBoundTo.Character,
            MandatoryStatus.Optional,
            showForGroups: [],
            validForNpc: true,
            includeInPrint: true,
            showForUnapprovedClaims: true,
            price: 0,
            masterFieldHint: "",
            programmaticValue: null));

        var variant = await fieldSetupService.CreateFieldValueVariant(new CreateFieldValueVariantRequest(
            fieldId,
            "Смоук-значение",
            description: null,
            masterDescription: null,
            programmaticValue: null,
            price: 0,
            playerSelectable: true,
            timeSlotOptions: null));

        return (fieldId, variant.ProjectFieldVariantId);
    }

    /// <summary>Названия комнат сида — их же ждёт на странице сценарий расселения.</summary>
    private static readonly string[] SeededRoomNames = ["1", "2"];

    /// <summary>Вместимость комнаты сида — из неё сценарии считают ожидаемые счётчики занятости.</summary>
    private const int SeededRoomCapacity = 4;

    /// <summary>
    /// Тип проживания с парой комнат: тип — настройка проекта (ADR015), комнаты — нет.
    /// </summary>
    private static async Task<int> SeedRoomTypeAsync(IServiceProvider sp, ProjectIdentification projectId)
    {
        var roomTypeId = await sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
            projectId,
            new AccommodationTypeRequest(
                "Смоук-палатка",
                new MarkdownString("Палатка для смоука"),
                Cost: 100,
                Capacity: SeededRoomCapacity,
                IsPlayerSelectable: true));

        // Комнаты добавляются в категорию, а не в тип проживания; категорию по типу знают
        // только метаданные (ADR018, §2).
        var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
        _ = await sp.GetRequiredService<IAccommodationService>()
            .AddRooms(
                projectInfo.AccommodationSettings.GetTypeById(roomTypeId).RoomCategoryId,
                SeededRoomNames);

        return roomTypeId.AccommodationTypeId;
    }

    /// <summary>
    /// Заявки на проживание: одна расселённая комната и один нерасселённый жилец (#5070).
    /// </summary>
    /// <remarks>
    /// Без этого сид давал тип поселения без жильцов, страница
    /// <c>rooms/{roomTypeId}/details</c> рендерила пустые списки — и N+1 по жильцам (по одной
    /// догрузке персонажа, игрока и денежных операций на заявку) в снапшот не попадал вовсе,
    /// хотя на проде это самый дорогой маршрут суток. Поэтому заявки на проживание есть у всех
    /// заявок сида, а не у одной: на одном жильце N+1 не отличить от одиночного запроса.
    /// </remarks>
    /// <returns>Сколько заявок на проживание расселено по комнатам.</returns>
    private Task<int> SeedAccommodationRequestsAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId,
        SeededContent seeded,
        IReadOnlyList<ClaimIdentification> claims)
        => Factory.Services.RunAsAsync(ownerId, async sp =>
        {
            // Сервис добавления комнат не отдаёт их id наружу, поэтому берём комнату из базы.
            var roomId = sp.GetRequiredService<MyDbContext>().Set<ProjectAccommodation>()
                .Where(r => r.ProjectId == projectId.Value && r.AccommodationTypeId == seeded.RoomTypeId)
                .OrderBy(r => r.Id)
                .Select(r => r.Id)
                .First();

            var claimService = sp.GetRequiredService<IClaimService>();
            var requestIds = new List<int>(claims.Count);
            foreach (var claimId in claims)
            {
                var request = await claimService.SetAccommodationType(
                    projectId.Value, claimId.ClaimId, seeded.RoomTypeId);
                requestIds.Add(request.Id);
            }

            // Часть жильцов расселена, часть — нет: страница показывает и комнаты с жильцами,
            // и список нерасселённых, а в отчёте по расселению встречаются обе строки.
            var placed = requestIds.SkipLast(1).ToList();
            await sp.GetRequiredService<IAccommodationService>().OccupyRoom(
                new AccommodationRoomIdentification(projectId, roomId),
                [.. placed.Select(id => new AccommodationRequestIdentification(projectId, id))]);

            return placed.Count;
        });

    private static async Task<int> SeedSubscriptionAsync(
        IServiceProvider sp,
        ProjectIdentification projectId,
        CharacterGroupIdentification groupId,
        UserIdentification masterId)
    {
        await sp.GetRequiredService<IGameSubscribeService>().UpdateSubscribeForGroup(new SubscribeForGroupRequest
        {
            CharacterGroupId = groupId,
            SubscriptionOptions = SubscriptionOptions.CreateAllSet(),
            MasterId = masterId.Value,
        });

        // Сервис подписки не возвращает id созданной записи, а он нужен странице редактирования.
        return sp.GetRequiredService<MyDbContext>().Set<UserSubscription>()
            .Where(s => s.ProjectId == projectId.Value && s.UserId == masterId.Value)
            .Select(s => s.UserSubscriptionId)
            .First();
    }

    /// <summary>
    /// Взнос и приём наличных: без них не открываются финансовые страницы и не заводятся операции.
    /// </summary>
    private static async Task<PaymentTypeIdentification> SeedFinancesAsync(
        IServiceProvider sp,
        ProjectIdentification projectId,
        UserIdentification ownerId)
    {
        var financeSettingsService = sp.GetRequiredService<IProjectFinanceSettingsService>();

        await financeSettingsService.CreateFeeSetting(new CreateFeeSettingRequest
        {
            ProjectId = projectId,
            Fee = 1000,
            // Льготный взнос требует отдельного флага в настройках финансов — смоуку он не нужен.
            PreferentialFee = null,
            // Задним числом взнос не заводится — сервис такое запрещает.
            StartDate = DateTime.UtcNow.Date.AddDays(1),
        });
        await financeSettingsService.CreatePaymentType(new CreatePaymentTypeRequest
        {
            ProjectId = projectId,
            TargetMasterId = ownerId,
            TypeKind = PaymentTypeKind.Cash,
            Name = null,
        });

        return GetPaymentType(sp, projectId, ownerId);
    }

    private static PaymentTypeIdentification GetPaymentType(
        IServiceProvider sp,
        ProjectIdentification projectId,
        UserIdentification ownerId)
    {
        // Сервис создания типа оплаты не возвращает id, а он нужен и странице, и приёму взноса.
        var paymentTypeId = sp.GetRequiredService<MyDbContext>().Set<PaymentType>()
            .Where(p => p.ProjectId == projectId.Value && p.UserId == ownerId.Value)
            .Select(p => p.PaymentTypeId)
            .First();

        return new PaymentTypeIdentification(projectId, paymentTypeId);
    }

    /// <summary>
    /// Заявки от разных игроков в разных статусах: принятая с оплатой, отклонённая с оплатой и
    /// на рассмотрении.
    /// </summary>
    /// <remarks>
    /// Разные статусы нужны, чтобы страницы списков заявок (их в приложении больше десятка)
    /// открывались не на пустом наборе: на пустом списке N+1 не проявляется, а именно он и
    /// ищется. Оплата у отклонённой заявки — для PaidDeclined: фильтр страницы требует заявку
    /// «отклонена, но баланс больше нуля», и пока такой строки в сиде не было, маршрут мерился
    /// по пустому списку и в снапшот не попадал (#5135). Отложенного статуса в сиде намеренно нет:
    /// он покрывался той же строкой, что DeclinedList, а отдельная заявка ради OnHoldList
    /// добавила бы ещё одного жильца и перекроила бы половину снапшота.
    /// </remarks>
    private async Task<SeededClaims> SeedClaimsAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId,
        IReadOnlyList<CharacterIdentification> characters)
    {
        var claims = new List<ClaimIdentification>(characters.Count);
        var playerIds = new List<UserIdentification>(characters.Count);

        foreach (var characterId in characters)
        {
            var (playerId, _) = await CreateUserAsync();
            await FillPlayerProfileAsync(playerId);
            playerIds.Add(playerId);
            claims.Add(await Factory.Services.RunAsAsync(playerId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                    characterId,
                    "Хочу играть эту роль",
                    FieldLayerContainer.Empty(projectInfo),
                    sensitiveDataAllowed: true);
            }));
        }

        return await Factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();

            await claimService.ApproveByMaster(claims[0], "Принято");
            await claimService.AddComment(
                claims[2], parentCommentId: null, isVisibleToPlayer: true, "Обсуждаем");

            // Вторая заявка проходит полный путь «принята → оплачена → отклонена»: строка с деньгами
            // нужна PaidDeclined. Отклонять надо до заявок на проживание — иначе отклонение удалит
            // заявку на проживание (ConsiderLeavingRoom), а сид хочет её и у отклонённой.
            await claimService.ApproveByMaster(claims[1], "Принято, но потом отклоним");

            var paymentTypeId = GetPaymentType(sp, projectId, ownerId);
            var financeService = sp.GetRequiredService<IFinanceService>();
            await financeService.FeeAcceptedOperation(new FeeAcceptedOperationRequest
            {
                ClaimId = claims[0].ClaimId,
                Contents = "Взнос за смоук",
                OperationDate = DateTime.UtcNow.Date,
                Money = 100,
                PaymentTypeId = paymentTypeId,
            });
            await financeService.FeeAcceptedOperation(new FeeAcceptedOperationRequest
            {
                ClaimId = claims[1].ClaimId,
                Contents = "Взнос до отклонения",
                OperationDate = DateTime.UtcNow.Date,
                Money = 100,
                PaymentTypeId = paymentTypeId,
            });

            await claimService.DeclineByMaster(
                claims[1],
                ClaimDenialReason.NotSuitable,
                "Не подошла роль",
                deleteCharacter: false);

            var db = sp.GetRequiredService<MyDbContext>();
            var discussion = db.Set<CommentDiscussion>()
                .Where(d => d.ProjectId == projectId.Value && d.Comments.Any())
                .OrderBy(d => d.CommentDiscussionId)
                .Select(d => new
                {
                    d.CommentDiscussionId,
                    CommentId = d.Comments.OrderBy(c => c.CommentId).Select(c => c.CommentId).FirstOrDefault(),
                })
                .First();
            var financeOperationId = db.Set<FinanceOperation>()
                .Where(o => o.ProjectId == projectId.Value)
                .OrderBy(o => o.CommentId)
                .Select(o => o.CommentId)
                .First();

            return new SeededClaims(
                claims[0],
                claims,
                playerIds,
                playerIds[1],
                discussion.CommentDiscussionId,
                discussion.CommentId,
                financeOperationId);
        });
    }

    /// <summary>
    /// Заполняет профиль игрока: имя и телефон.
    /// </summary>
    /// <remarks>
    /// Телефон нужен отчёту по расселению — он печатает колонку из <c>UserExtra</c>, и на пустом
    /// профиле колонка была бы пустой, то есть тест не проверял бы её содержимое (#5070).
    /// </remarks>
    private Task FillPlayerProfileAsync(UserIdentification playerId)
        => Factory.Services.RunAsAsync(playerId, async sp =>
        {
            await sp.GetRequiredService<IUserService>().UpdateProfile(
                playerId.Value,
                new UserFullName(
                    new PrefferedName(DisplayNameFor(playerId)),
                    new BornName("Иван"),
                    new SurName($"Игроков{playerId.Value}"),
                    new FatherName("Иванович")),
                Gender.Male,
                phoneNumber: PhoneNumberFor(playerId),
                nicknames: "",
                groupNames: "",
                livejournal: "",
                ContactsAccessType.OnlyForMasters,
                passportData: "",
                registrationAddress: "",
                birthDate: null);
        });

    /// <summary>Телефон игрока — он же ожидаемое содержимое колонки в отчёте по расселению.</summary>
    private static string PhoneNumberFor(UserIdentification playerId) => $"+7900{playerId.Value:0000000}";

    /// <summary>Отображаемое имя игрока: сайт показывает предпочитаемое имя.</summary>
    private static string DisplayNameFor(UserIdentification playerId) => $"Смоук-игрок {playerId.Value}";

    private sealed record SeededContent(
        CharacterGroupIdentification RootGroupId,
        CharacterGroupIdentification GroupId,
        IReadOnlyList<CharacterIdentification> Characters,
        ProjectFieldIdentification DropdownFieldId,
        int DropdownVariantId,
        PlotSeedResult Plot,
        ForumThreadIdentification ForumThreadId,
        int RoomTypeId,
        ProjectRolesListIdentification RolesListId,
        int SubscriptionId,
        PaymentTypeIdentification PaymentTypeId);

    private sealed record SeededClaims(
        ClaimIdentification ApprovedClaimId,
        IReadOnlyList<ClaimIdentification> AllClaims,
        IReadOnlyList<UserIdentification> PlayerIds,
        UserIdentification DeclinedPlayerId,
        int CommentDiscussionId,
        int CommentId,
        int FinanceOperationId);
}
