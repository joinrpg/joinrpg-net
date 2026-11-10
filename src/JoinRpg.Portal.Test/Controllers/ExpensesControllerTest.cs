using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Export;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Finances.Expenses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Ручки расходов: доменные <see cref="ExpenseInfo"/> превращаются в <see cref="Web.Finances.Expenses.ExpenseViewModel"/>
/// с путём категории и именами мастеров из метаданных проекта, «мои расходы» — по текущему пользователю,
/// создание склеивает форму с проектом; согласование требует <see cref="Permission.CanManageMoney"/>.
/// </summary>
public class ExpensesControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);

    // Совпадает с UserId мастера по умолчанию в MockedProject; важно только для currentUserId.
    private static readonly UserIdentification MasterId = new(2);

    [Fact]
    public async Task ListMapsDomainToDtos()
    {
        // Имена берутся из ProjectInfo — заводим обоих мастеров в моке проекта.
        var mock = new MockedProject();
        var otherMaster = mock.CreateMaster("Мастера Маша");
        mock.ReInitProjectInfo();
        var masterId = new UserIdentification(mock.Master.UserId);
        var expense = new ExpenseInfo(
            new ExpenseIdentification(ProjectId, 7),
            Name: "Аренда столов",
            Description: new MarkdownString("**Договор** с базой"),
            CategoryPath:
            [
                new ExpenseCategoryBriefInfo(new ExpenseCategoryIdentification(ProjectId, 1), "Аренда базы"),
                new ExpenseCategoryBriefInfo(new ExpenseCategoryIdentification(ProjectId, 2), "Инвентарь"),
            ],
            PlannedQuantity: 10,
            PlannedPrice: 300,
            ActualQuantity: null,
            ActualPrice: null,
            SpentAt: null,
            State: ExpenseState.Proposed,
            MasterId: masterId,
            CreatedById: new(otherMaster.UserId));
        var controller = CreateController(
            new FakeExpenseRepository { Expenses = [expense] },
            metadataRepository: new FakeProjectMetadataRepository(mock.ProjectInfo));

        var result = await controller.List(ProjectId);

        var dto = result.Result.ShouldBeOfType<OkObjectResult>().Value
            .ShouldBeAssignableTo<IReadOnlyList<ExpenseViewModel>>()!.Single();
        dto.ExpenseId.ShouldBe(expense.ExpenseId);
        dto.Name.ShouldBe("Аренда столов");
        dto.CategoryPath.ShouldBe("Аренда базы\\Инвентарь");
        dto.Description.ShouldBe("**Договор** с базой");
        dto.State.ShouldBe(ExpenseStateView.Proposed);
        dto.MasterName.ShouldBe("Master");
        dto.CreatedByName.ShouldBe("Мастера Маша");
        dto.PlannedCost.ShouldBe(3000);
    }

    [Fact]
    public async Task MyLoadsExpensesOfCurrentUser()
    {
        var repository = new FakeExpenseRepository();
        var controller = CreateController(repository, currentUserId: MasterId);

        _ = await controller.My(ProjectId);

        repository.ByMaster.ShouldBe(MasterId);
    }

    [Fact]
    public async Task CreatePassesFormToService()
    {
        var service = new FakeExpenseService();
        var controller = CreateController(service: service);
        var spentAt = new DateOnly(2026, 2, 15);
        var form = new ExpenseEditViewModel
        {
            MasterId = MasterId,
            Name = "Аренда столов",
            CategoryPath = "Аренда базы\\Инвентарь",
            PlannedQuantity = 10,
            PlannedPrice = 300,
            SpentAt = spentAt,
            Description = "описание",
        };

        var result = await controller.Create(ProjectId, form);

        result.Result.ShouldBeOfType<OkObjectResult>().Value
            .ShouldBe(new ExpenseIdentification(ProjectId, 42));
        var request = service.Created.ShouldBeOfType<(ProjectIdentification, CreateExpenseRequest)>();
        request.Item1.ShouldBe(ProjectId);
        request.Item2.MasterId.ShouldBe(MasterId);
        request.Item2.Name.ShouldBe("Аренда столов");
        request.Item2.CategoryPath.ShouldBe("Аренда базы\\Инвентарь");
        request.Item2.PlannedQuantity.ShouldBe(10);
        request.Item2.PlannedPrice.ShouldBe(300);
        request.Item2.SpentAt.ShouldBe(spentAt);
        request.Item2.Description.ShouldBe(new MarkdownString("описание"));
    }

    [Fact]
    public async Task CreateWithMoneyRightsApprovesExpenseAfterCreation()
    {
        // Финансовый администратор: отдельный вызов ApproveExpense сразу после создания
        var mock = new MockedProject();
        var service = new FakeExpenseService();
        var controller = CreateController(
            service: service,
            metadataRepository: new FakeProjectMetadataRepository(mock.ProjectInfo),
            currentUserId: new UserIdentification(mock.Master.UserId));

        var result = await controller.Create(ProjectId, CreateForm());

        result.Result.ShouldBeOfType<OkObjectResult>().Value
            .ShouldBe(new ExpenseIdentification(ProjectId, 42));
        _ = service.Created.ShouldBeOfType<(ProjectIdentification, CreateExpenseRequest)>();
        var approved = service.Approved.ShouldBeOfType<(ExpenseIdentification, MarkdownString?)>();
        approved.Item1.ShouldBe(new ExpenseIdentification(ProjectId, 42));
        approved.Item2.ShouldBeNull();
    }

    [Fact]
    public async Task CreateWithoutMoneyRightsLeavesExpenseProposed()
    {
        var mock = new MockedProject();
        var masterWithoutMoney = mock.CreateMaster("Безденежный");
        mock.Project.ProjectAcls.Single(acl => acl.UserId == masterWithoutMoney.UserId).CanManageMoney = false;
        mock.ReInitProjectInfo();
        var service = new FakeExpenseService();
        var controller = CreateController(
            service: service,
            metadataRepository: new FakeProjectMetadataRepository(mock.ProjectInfo),
            currentUserId: new UserIdentification(masterWithoutMoney.UserId));

        _ = await controller.Create(ProjectId, CreateForm());

        _ = service.Created.ShouldBeOfType<(ProjectIdentification, CreateExpenseRequest)>();
        service.Approved.ShouldBeNull();
    }

    private static ExpenseEditViewModel CreateForm()
        => new()
        {
            MasterId = MasterId,
            Name = "Аренда столов",
            CategoryPath = "Аренда базы\\Инвентарь",
            PlannedQuantity = 10,
            PlannedPrice = 300,
        };

    [Fact]
    public async Task EditPassesTypedIdAndMapsForm()
    {
        var service = new FakeExpenseService();
        var controller = CreateController(service: service);
        var spentAt = new DateOnly(2026, 3, 1);
        var form = new ExpenseEditViewModel
        {
            ExpenseId = new ExpenseIdentification(ProjectId, 7),
            MasterId = MasterId,
            Name = "Аренда столов",
            CategoryPath = "Аренда базы\\Инвентарь",
            PlannedQuantity = 12,
            PlannedPrice = 350,
            SpentAt = spentAt,
            Description = "новое описание",
        };

        var result = await controller.Edit(ProjectId, new ExpenseIdentification(ProjectId, 7), form);

        _ = result.ShouldBeOfType<OkResult>();
        var edited = service.Edited.ShouldBeOfType<(ExpenseIdentification Id, EditExpenseRequest Request)>();
        edited.Id.ShouldBe(new ExpenseIdentification(ProjectId, 7));
        edited.Request.MasterId.ShouldBe(MasterId);
        edited.Request.Name.ShouldBe("Аренда столов");
        edited.Request.CategoryPath.ShouldBe("Аренда базы\\Инвентарь");
        edited.Request.PlannedQuantity.ShouldBe(12);
        edited.Request.PlannedPrice.ShouldBe(350);
        edited.Request.SpentAt.ShouldBe(spentAt);
        edited.Request.Description.ShouldBe(new MarkdownString("новое описание"));
    }

    [Fact]
    public async Task SetActualPassesRequestToService()
    {
        var service = new FakeExpenseService();
        var controller = CreateController(service: service);
        var spentAt = new DateOnly(2026, 2, 15);

        var result = await controller.SetActual(ProjectId, new ExpenseIdentification(ProjectId, 7), new ExpenseActualRequest(2, 1400, spentAt));

        _ = result.ShouldBeOfType<OkResult>();
        var actual = service.ActualSet.ShouldBeOfType<(ExpenseIdentification Id, ExpenseActualRequest Request)>();
        actual.Id.ShouldBe(new ExpenseIdentification(ProjectId, 7));
        actual.Request.ActualQuantity.ShouldBe(2);
        actual.Request.ActualPrice.ShouldBe(1400);
        actual.Request.SpentAt.ShouldBe(spentAt);
    }

    [Fact]
    public async Task CategoryPathSuggestionsPassesServiceResultThrough()
    {
        var controller = CreateController(service: new FakeExpenseService { Suggestions = ["Аренда базы", "Транспорт"] });

        var result = await controller.CategoryPathSuggestions(ProjectId);

        result.Result.ShouldBeOfType<OkObjectResult>().Value
            .ShouldBeAssignableTo<IReadOnlyCollection<string>>()!.ShouldBe(["Аренда базы", "Транспорт"]);
    }

    [Fact]
    public async Task ExportReturnsXlsxFileWithExpenses()
    {
        var mock = new MockedProject();
        var masterId = new UserIdentification(mock.Master.UserId);
        var expense = new ExpenseInfo(
            new ExpenseIdentification(ProjectId, 7),
            Name: "Аренда столов",
            Description: new MarkdownString("**Договор** с базой"),
            CategoryPath: [new ExpenseCategoryBriefInfo(new ExpenseCategoryIdentification(ProjectId, 1), "Аренда базы")],
            PlannedQuantity: 10,
            PlannedPrice: 300,
            ActualQuantity: null,
            ActualPrice: null,
            SpentAt: null,
            State: ExpenseState.Approved,
            MasterId: masterId,
            CreatedById: masterId);
        var controller = CreateController(
            repository: new FakeExpenseRepository { Expenses = [expense] },
            metadataRepository: new FakeProjectMetadataRepository(mock.ProjectInfo));

        var result = await controller.Export(ProjectId, "xlsx");

        var file = result.ShouldBeOfType<FileContentResult>();
        file.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        file.FileDownloadName.ShouldEndWith(".xlsx");
        file.FileDownloadName.ShouldContain("Расходы");
        file.FileContents.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExportReturnsCsvFileWhenRequested()
    {
        var controller = CreateController();

        var result = await controller.Export(ProjectId, "csv");

        var file = result.ShouldBeOfType<FileContentResult>();
        file.ContentType.ShouldBe("text/csv");
        file.FileDownloadName.ShouldEndWith(".csv");
        file.FileDownloadName.ShouldContain("Расходы");
        file.FileContents.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExportWithoutExpensesReturnsNonEmptyXlsx()
    {
        var controller = CreateController();

        var result = await controller.Export(ProjectId, "xlsx");

        var file = result.ShouldBeOfType<FileContentResult>();
        file.FileDownloadName.ShouldEndWith(".xlsx");
        file.FileContents.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CanManageMoneyComesFromMasterPermissions()
    {
        var mock = new MockedProject();
        var repository = new FakeProjectMetadataRepository(mock.ProjectInfo);
        var masterController = CreateController(metadataRepository: repository, currentUserId: new UserIdentification(mock.Master.UserId));
        var playerController = CreateController(metadataRepository: repository, currentUserId: new UserIdentification(mock.Player.UserId));

        var masterResult = await masterController.CanManageMoney(mock.ProjectInfo.ProjectId);
        var playerResult = await playerController.CanManageMoney(mock.ProjectInfo.ProjectId);

        masterResult.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBe(true);
        playerResult.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBe(false);
    }

    [Fact]
    public void ApproveAndRejectRequireCanManageMoney()
    {
        foreach (var action in new[] { nameof(ExpensesController.Approve), nameof(ExpensesController.Reject) })
        {
            var attribute = typeof(ExpensesController).GetMethod(action)!
                .GetCustomAttributes(typeof(RequireMaster), inherit: false)
                .Single().ShouldBeOfType<RequireMaster>();
            attribute.Policy.ShouldBe($"{nameof(RequireMaster)}__{Permission.CanManageMoney}");
        }
    }

    private static readonly IExportDataService ExportDataService =
        new ServiceCollection().AddJoinExportService().BuildServiceProvider().GetRequiredService<IExportDataService>();

    private static ExpensesController CreateController(
        FakeExpenseRepository? repository = null,
        FakeExpenseService? service = null,
        FakeProjectMetadataRepository? metadataRepository = null,
        UserIdentification? currentUserId = null)
        => new(
            repository ?? new FakeExpenseRepository(),
            service ?? new FakeExpenseService(),
            new FakeCurrentUserAccessor(currentUserId ?? MasterId),
            metadataRepository ?? new FakeProjectMetadataRepository(new MockedProject().ProjectInfo),
            ExportDataService,
            new StubUriService());

    private sealed class FakeExpenseRepository : IExpenseRepository
    {
        public IReadOnlyCollection<ExpenseInfo> Expenses { get; init; } = [];

        public UserIdentification? ByMaster { get; private set; }

        public Task<IReadOnlyCollection<ExpenseInfo>> GetExpenses(ProjectIdentification projectId)
            => Task.FromResult(Expenses);

        public Task<IReadOnlyCollection<ExpenseInfo>> GetExpensesByMaster(ProjectIdentification projectId, UserIdentification masterId)
        {
            ByMaster = masterId;
            return Task.FromResult(Expenses);
        }

        public Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
            => throw new NotSupportedException();
    }

    private sealed class FakeExpenseService : IExpenseService
    {
        public object? Created { get; private set; }

        public object? Edited { get; private set; }

        public object? ActualSet { get; private set; }

        public object? Approved { get; private set; }

        public Task<ExpenseIdentification> CreateExpense(ProjectIdentification projectId, CreateExpenseRequest request)
        {
            Created = (projectId, request);
            return Task.FromResult(new ExpenseIdentification(projectId, 42));
        }

        public Task EditExpense(ExpenseIdentification expenseId, EditExpenseRequest request)
        {
            Edited = (expenseId, request);
            return Task.CompletedTask;
        }

        public Task SetExpenseActual(ExpenseIdentification expenseId, ExpenseActualRequest request)
        {
            ActualSet = (expenseId, request);
            return Task.CompletedTask;
        }

        public Task ApproveExpense(ExpenseIdentification expenseId, MarkdownString? comment)
        {
            Approved = (expenseId, comment);
            return Task.CompletedTask;
        }

        public Task RejectExpense(ExpenseIdentification expenseId, MarkdownString? comment) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<string>> LoadCategoryPathSuggestions(ProjectIdentification projectId)
            => Task.FromResult(Suggestions);

        public IReadOnlyCollection<string> Suggestions { get; init; } = [];
    }

    private sealed class FakeProjectMetadataRepository(ProjectInfo projectInfo) : IProjectMetadataRepository
    {
        public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
            => Task.FromResult(projectInfo);

        public Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectInfo projectInfo) => throw new NotSupportedException();

        public void PrimeCache(ProjectInfo projectInfo)
        {
        }
    }

    /// <summary>Ссылки не используются в выгрузке расходов — заглушка, чтобы собрать экспортёр.</summary>
    private sealed class StubUriService : IUriService
    {
        public string Get(ILinkable link) => GetUri(link).ToString();

        public Uri GetUri(ILinkable link) => new("https://joinrpg.ru/");
    }
}
