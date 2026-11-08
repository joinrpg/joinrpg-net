using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты сервиса расходов проекта (ADR030). ТДД: написаны против контракта
/// <see cref="IExpenseService"/>, сервис <c>ExpenseService</c> появится позже.
/// </summary>
public class ExpenseServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly FakeProjectMetadataRepository metadataRepository;
    private readonly FakeExpenseRepository expenseRepository;

    private readonly List<Expense> expenses = [];
    private readonly List<ExpenseCategory> categories = [];
    private readonly List<ExpenseApproval> approvals = [];

    public ExpenseServiceTest()
    {
        // Мок проекта по умолчанию имеет ProjectId = 0 — как у несохранённой сущности,
        // поэтому баг «не проставлен ProjectId у расхода» на нём невиден. Задаём ненулевой.
        mock.Project.ProjectId = 4;
        mock.ReInitProjectInfo();
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
        expenseRepository = new FakeExpenseRepository(categories);
        unitOfWork.UseDbSet<Expense>(expenses);
        unitOfWork.UseDbSet<ExpenseCategory>(categories);
        unitOfWork.UseDbSet<ExpenseApproval>(approvals);
    }

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private ExpenseService CreateService(int currentUserId, bool isAdmin = false)
        => new(
            unitOfWork,
            metadataRepository,
            new FakeCurrentUserAccessor(currentUserId, isAdmin),
            expenseRepository,
            new ExpenseCategoryResolver(unitOfWork, expenseRepository, NullLogger<ExpenseCategoryResolver>.Instance),
            NullLogger<ExpenseService>.Instance);

    private static CreateExpenseRequest CreateRequest(
        UserIdentification masterId,
        string categoryPath = "Аренда базы\\Жилые корпуса",
        int plannedQuantity = 2,
        int plannedPrice = 5000)
        => new(masterId, "Аренда домика", categoryPath, plannedQuantity, plannedPrice, new MarkdownString("Предоплата за август"));

    private static EditExpenseRequest EditRequest(
        UserIdentification masterId,
        string name = "Аренда домика (уточнено)",
        int plannedQuantity = 3,
        int plannedPrice = 4500)
        => new(masterId, name, "Аренда базы\\Жилые корпуса", plannedQuantity, plannedPrice, new MarkdownString("Уточнено"));

    /// <summary>Ещё один мастер проекта; метаданные пересобираем, чтобы ACL увидел его.</summary>
    private User CreateMaster(string name = "Master2")
    {
        var master = mock.CreateMaster(name);
        mock.ReInitProjectInfo();
        return master;
    }

    /// <summary>Мастер проекта без права распоряжаться деньгами.</summary>
    private User CreateMasterWithoutMoney(string name = "Безденежный")
    {
        var master = mock.CreateMaster(name);
        mock.Project.ProjectAcls.Single(acl => acl.UserId == master.UserId).CanManageMoney = false;
        mock.ReInitProjectInfo();
        return master;
    }

    private async Task<ExpenseIdentification> CreateExpenseAsMaster(int currentUserId = 0, int plannedQuantity = 2, int plannedPrice = 5000)
    {
        var masterId = currentUserId == 0 ? mock.Master.UserId : currentUserId;
        return await CreateService(masterId).CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(masterId), plannedQuantity: plannedQuantity, plannedPrice: plannedPrice));
    }

    [Fact]
    public async Task CreateExpense_CreatesCategoryPathAndSetsProposedState()
    {
        var service = CreateService(mock.Master.UserId);

        var expenseId = await service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Master.UserId)));

        var expense = expenses.ShouldHaveSingleItem();
        expenseId.ExpenseId.ShouldBe(expense.ExpenseId);
        expense.State.ShouldBe(ExpenseState.Proposed);
        expense.ProjectId.ShouldBe(ProjectId.Value);
        expense.MasterId.ShouldBe(mock.Master.UserId);
        expense.CreatedById.ShouldBe(mock.Master.UserId);
        expense.PlannedQuantity.ShouldBe(2);
        expense.PlannedPrice.ShouldBe(5000);

        categories.Count.ShouldBe(2);
        var root = categories.Single(c => c.Name == "Аренда базы");
        var leaf = categories.Single(c => c.Name == "Жилые корпуса");
        root.ParentCategoryId.ShouldBeNull();
        root.ParentCategoryIds.ShouldBeEmpty();
        leaf.ParentCategoryId.ShouldBe(root.ExpenseCategoryId);
        leaf.ParentCategoryIds.ShouldBe([root.ExpenseCategoryId]);

        // Снапшот пути от корня к листу, по нему грид фильтрует поддерево
        expense.CategoryId.ShouldBe(leaf.ExpenseCategoryId);
        expense.CategoryPathIds.ShouldBe([root.ExpenseCategoryId, leaf.ExpenseCategoryId]);
    }

    [Fact]
    public async Task ApproveExpense_ApprovesAndWritesApprovalRecord()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();

        await service.ApproveExpense(expenseId, new MarkdownString("Ок"));

        var expense = expenses.ShouldHaveSingleItem();
        expense.State.ShouldBe(ExpenseState.Approved);
        var approval = approvals.ShouldHaveSingleItem();
        approval.ExpenseId.ShouldBe(expense.ExpenseId);
        approval.State.ShouldBe(ExpenseState.Approved);
        approval.ApprovedById.ShouldBe(mock.Master.UserId);
        approval.Comment.Contents.ShouldBe("Ок");
    }

    [Fact]
    public async Task RejectExpense_RejectsAndWritesApprovalRecord()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();

        await service.RejectExpense(expenseId, new MarkdownString("Не по смете"));

        var expense = expenses.ShouldHaveSingleItem();
        expense.State.ShouldBe(ExpenseState.Rejected);
        var approval = approvals.ShouldHaveSingleItem();
        approval.State.ShouldBe(ExpenseState.Rejected);
        approval.ApprovedById.ShouldBe(mock.Master.UserId);
        approval.Comment.Contents.ShouldBe("Не по смете");
    }

    [Fact]
    public async Task EditExpense_ResetsApprovedExpenseToProposed()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();
        await service.ApproveExpense(expenseId, null);

        await service.EditExpense(
            expenseId,
            new EditExpenseRequest(
                new UserIdentification(mock.Master.UserId),
                "Аренда домика (уточнено)",
                "Аренда базы\\Жилые корпуса",
                3,
                4500,
                null));

        var expense = expenses.ShouldHaveSingleItem();
        expense.State.ShouldBe(ExpenseState.Proposed);
        expense.Name.ShouldBe("Аренда домика (уточнено)");
        expense.PlannedQuantity.ShouldBe(3);
        expense.PlannedPrice.ShouldBe(4500);
    }

    [Fact]
    public async Task EditExpense_DecreasesPlannedCost_KeepsApproved()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();
        await service.ApproveExpense(expenseId, null);

        // 1×5000=5000 меньше прежнего плана 2×5000=10000 — одобрение сохраняется
        await service.EditExpense(
            expenseId,
            EditRequest(new UserIdentification(mock.Master.UserId), plannedQuantity: 1, plannedPrice: 5000));

        var expense = expenses.ShouldHaveSingleItem();
        expense.State.ShouldBe(ExpenseState.Approved);
        expense.PlannedQuantity.ShouldBe(1);
        expense.PlannedPrice.ShouldBe(5000);
    }

    [Fact]
    public async Task EditExpense_SamePlannedCost_KeepsApproved()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();
        await service.ApproveExpense(expenseId, null);

        // 2×5000=10000 равно прежнему плану: денежная сторона не изменилась — одобрение сохраняется
        await service.EditExpense(
            expenseId,
            EditRequest(new UserIdentification(mock.Master.UserId), plannedQuantity: 2, plannedPrice: 5000));

        var expense = expenses.ShouldHaveSingleItem();
        expense.State.ShouldBe(ExpenseState.Approved);
    }

    [Fact]
    public async Task EditExpense_RejectedExpense_ResetsToProposed()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();
        await service.RejectExpense(expenseId, new MarkdownString("Не по смете"));

        // Отклонённый расход при правке снова уходит на рассмотрение, даже если план уменьшился
        await service.EditExpense(
            expenseId,
            EditRequest(new UserIdentification(mock.Master.UserId), plannedQuantity: 1, plannedPrice: 5000));

        var expense = expenses.ShouldHaveSingleItem();
        expense.State.ShouldBe(ExpenseState.Proposed);
    }

    [Fact]
    public async Task SetExpenseActual_ResetsApprovedExpenseToProposedAndStoresActual()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();
        await service.ApproveExpense(expenseId, null);

        var spentAt = new DateOnly(2026, 8, 20);
        // 3×5000=15000 больше плана 2×5000=10000 — одобрение сбрасывается
        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(3, 5000, spentAt));

        var expense = expenses.ShouldHaveSingleItem();
        expense.State.ShouldBe(ExpenseState.Proposed);
        expense.ActualQuantity.ShouldBe(3);
        expense.ActualPrice.ShouldBe(5000);
        expense.SpentAt.ShouldBe(spentAt.ToDateTime(TimeOnly.MinValue));
    }

    [Fact]
    public async Task ApproveExpense_WithoutMoneyPermission_ThrowsAndKeepsState()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();
        // Мастер без права распоряжаться деньгами
        var masterWithoutMoney = mock.CreateMaster("Безденежный");
        mock.Project.ProjectAcls.Single(acl => acl.UserId == masterWithoutMoney.UserId).CanManageMoney = false;
        mock.ReInitProjectInfo();
        var noMoneyService = CreateService(masterWithoutMoney.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => noMoneyService.ApproveExpense(expenseId, new MarkdownString("Ок")));

        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Proposed);
        approvals.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateExpense_ByPlayerNotInProject_Throws()
    {
        // Player (id 1) не входит в ACL проекта
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Player.UserId))));

        expenses.ShouldBeEmpty();
        categories.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateExpense_WithAnotherMasterId_RecordsThatMaster()
    {
        var service = CreateService(mock.Master.UserId);
        var otherMaster = CreateMaster();

        var expenseId = await service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(otherMaster.UserId)));

        var expense = expenses.ShouldHaveSingleItem();
        expenseId.ExpenseId.ShouldBe(expense.ExpenseId);
        expense.MasterId.ShouldBe(otherMaster.UserId);
        expense.CreatedById.ShouldBe(mock.Master.UserId);
    }

    [Fact]
    public async Task CreateExpense_WithMasterIdOfNonMaster_Throws()
    {
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(999))));

        expenses.ShouldBeEmpty();
        categories.ShouldBeEmpty();
    }

    [Fact]
    public async Task EditExpense_WithMasterIdOfNonMaster_ThrowsAndKeepsExpense()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster();
        var expense = expenses.ShouldHaveSingleItem();

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.EditExpense(
            expenseId,
            new EditExpenseRequest(
                new UserIdentification(999),
                "Новое имя",
                "Аренда базы\\Жилые корпуса",
                3,
                1000,
                null)));

        expense.Name.ShouldBe("Аренда домика");
        expense.PlannedQuantity.ShouldBe(2);
    }

    [Fact]
    public async Task CreateExpense_WithSamePathInOtherCase_ReusesExistingCategories()
    {
        var service = CreateService(mock.Master.UserId);
        await CreateExpenseAsMaster();

        await service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Master.UserId), categoryPath: "аренда базы\\Жилые корпуса"));

        // Новых категорий не создано — путь разрезолвился find-or-create
        categories.Count.ShouldBe(2);
        expenses.Count.ShouldBe(2);
        var secondExpense = expenses[1];
        secondExpense.CategoryPathIds.ShouldBe(expenses[0].CategoryPathIds);
    }

    [Fact]
    public async Task CreateExpense_WithNewLeafUnderExistingRoot_CreatesOnlyLeaf()
    {
        var service = CreateService(mock.Master.UserId);
        await CreateExpenseAsMaster();

        await service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Master.UserId), categoryPath: "Аренда базы\\Полигон"));

        // Создан только новый лист, корень переиспользован
        categories.Count.ShouldBe(3);
        var root = categories.Single(c => c.Name == "Аренда базы");
        categories.Single(c => c.Name == "Жилые корпуса");
        var polygon = categories.Single(c => c.Name == "Полигон");
        root.ParentCategoryId.ShouldBeNull();
        root.ParentCategoryIds.ShouldBeEmpty();
        polygon.ParentCategoryId.ShouldBe(root.ExpenseCategoryId);
        polygon.ParentCategoryIds.ShouldBe([root.ExpenseCategoryId]);

        var secondExpense = expenses[1];
        secondExpense.CategoryId.ShouldBe(polygon.ExpenseCategoryId);
        secondExpense.CategoryPathIds.ShouldBe([root.ExpenseCategoryId, polygon.ExpenseCategoryId]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateExpense_WithEmptyCategoryPath_Throws(string categoryPath)
    {
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<JoinValidationException>(() => service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Master.UserId), categoryPath: categoryPath)));

        expenses.ShouldBeEmpty();
        categories.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateExpense_WithMessyCategoryPath_NormalizesWithoutDuplicates()
    {
        var service = CreateService(mock.Master.UserId);
        await CreateExpenseAsMaster();

        // Лишние пробелы вокруг сегментов и двойной разделитель
        await service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Master.UserId), categoryPath: "  Аренда базы \\ Жилые корпуса "));

        categories.Count.ShouldBe(2);
        expenses.Count.ShouldBe(2);
        expenses[1].CategoryPathIds.ShouldBe(expenses[0].CategoryPathIds);
    }

    [Fact]
    public async Task CreateExpense_WithRepeatedSegmentNameInPath_CreatesDistinctLevels()
    {
        var service = CreateService(mock.Master.UserId);

        await service.CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Master.UserId), categoryPath: "А\\Б\\А"));

        // Три уровня, а не два: повтор имени «А» внутри пути — это лист, а не дубль корня
        categories.Count.ShouldBe(3);
        var rootA = categories.Single(c => c.Name == "А" && c.ParentCategoryId is null);
        var b = categories.Single(c => c.Name == "Б");
        var leafA = categories.Single(c => c.Name == "А" && c.ParentCategoryId is not null);
        b.ParentCategoryId.ShouldBe(rootA.ExpenseCategoryId);
        b.ParentCategoryIds.ShouldBe([rootA.ExpenseCategoryId]);
        leafA.ParentCategoryId.ShouldBe(b.ExpenseCategoryId);
        leafA.ParentCategoryIds.ShouldBe([rootA.ExpenseCategoryId, b.ExpenseCategoryId]);

        var expense = expenses.ShouldHaveSingleItem();
        expense.CategoryId.ShouldBe(leafA.ExpenseCategoryId);
        expense.CategoryPathIds.ShouldBe([rootA.ExpenseCategoryId, b.ExpenseCategoryId, leafA.ExpenseCategoryId]);
    }

    [Fact]
    public async Task EditExpense_WithoutMoneyRights_OwnExpense_Succeeds()
    {
        var owner = CreateMasterWithoutMoney();
        var expenseId = await CreateExpenseAsMaster(owner.UserId);
        var service = CreateService(owner.UserId);

        await service.EditExpense(expenseId, EditRequest(new UserIdentification(owner.UserId)));

        var expense = expenses.ShouldHaveSingleItem();
        expense.Name.ShouldBe("Аренда домика (уточнено)");
        expense.PlannedQuantity.ShouldBe(3);
        expense.PlannedPrice.ShouldBe(4500);
    }

    [Fact]
    public async Task EditExpense_WithoutMoneyRights_CreatedByHimButMasteredByOther_Succeeds()
    {
        var creator = CreateMasterWithoutMoney();
        // creator заводит расход, понесённый главным мастером: мастер — другой, автор — он
        var expenseId = await CreateService(creator.UserId).CreateExpense(
            ProjectId,
            CreateRequest(new UserIdentification(mock.Master.UserId)));
        var service = CreateService(creator.UserId);

        await service.EditExpense(expenseId, EditRequest(new UserIdentification(mock.Master.UserId)));

        var expense = expenses.ShouldHaveSingleItem();
        expense.MasterId.ShouldBe(mock.Master.UserId);
        expense.Name.ShouldBe("Аренда домика (уточнено)");
    }

    [Fact]
    public async Task EditExpense_WithoutMoneyRights_ForeignExpense_ThrowsAndKeepsExpense()
    {
        var owner = CreateMasterWithoutMoney();
        var expenseId = await CreateExpenseAsMaster(owner.UserId);
        var foreign = CreateMasterWithoutMoney("Чужой");
        var service = CreateService(foreign.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.EditExpense(
            expenseId,
            EditRequest(new UserIdentification(owner.UserId))));

        var expense = expenses.ShouldHaveSingleItem();
        expense.Name.ShouldBe("Аренда домика");
        expense.PlannedQuantity.ShouldBe(2);
        expense.PlannedPrice.ShouldBe(5000);
    }

    [Fact]
    public async Task EditExpense_WithoutMoneyRights_ReassignMaster_ThrowsAndKeepsMaster()
    {
        var owner = CreateMasterWithoutMoney();
        var expenseId = await CreateExpenseAsMaster(owner.UserId);
        var other = CreateMaster();
        var service = CreateService(owner.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.EditExpense(
            expenseId,
            EditRequest(new UserIdentification(other.UserId))));

        expenses.ShouldHaveSingleItem().MasterId.ShouldBe(owner.UserId);
    }

    [Fact]
    public async Task EditExpense_WithMoneyRights_EditsForeignAndReassignsMaster()
    {
        var owner = CreateMasterWithoutMoney();
        var expenseId = await CreateExpenseAsMaster(owner.UserId);
        var newMaster = CreateMaster("Master3");
        // Главный мастер — с CanManageMoney, чужой расход и смена мастера ему доступны
        var service = CreateService(mock.Master.UserId);

        await service.EditExpense(expenseId, EditRequest(new UserIdentification(newMaster.UserId)));

        var expense = expenses.ShouldHaveSingleItem();
        expense.MasterId.ShouldBe(newMaster.UserId);
        expense.Name.ShouldBe("Аренда домика (уточнено)");
    }

    [Fact]
    public async Task SetExpenseActual_WithoutMoneyRights_OwnExpense_Succeeds()
    {
        var owner = CreateMasterWithoutMoney();
        var expenseId = await CreateExpenseAsMaster(owner.UserId);
        var service = CreateService(owner.UserId);

        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(2, 4800, new DateOnly(2026, 8, 20)));

        var expense = expenses.ShouldHaveSingleItem();
        expense.ActualQuantity.ShouldBe(2);
        expense.ActualPrice.ShouldBe(4800);
    }

    [Fact]
    public async Task SetExpenseActual_WithoutMoneyRights_ForeignExpense_ThrowsAndKeepsActual()
    {
        var owner = CreateMasterWithoutMoney();
        var expenseId = await CreateExpenseAsMaster(owner.UserId);
        var foreign = CreateMasterWithoutMoney("Чужой");
        var service = CreateService(foreign.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.SetExpenseActual(
            expenseId,
            new ExpenseActualRequest(2, 4800, new DateOnly(2026, 8, 20))));

        var expense = expenses.ShouldHaveSingleItem();
        expense.ActualQuantity.ShouldBeNull();
        expense.ActualPrice.ShouldBeNull();
    }

    [Fact]
    public async Task SetExpenseActual_DecreasesApprovedActual_KeepsApproved()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster(plannedQuantity: 1, plannedPrice: 500);
        await service.ApproveExpense(expenseId, null);
        // 5×200=1000 больше плана 500 — согласование сбросилось, одобряем повторно
        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(5, 200, new DateOnly(2026, 8, 20)));
        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Proposed);
        await service.ApproveExpense(expenseId, null);

        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(4, 200, new DateOnly(2026, 8, 21)));

        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Approved);
    }

    [Fact]
    public async Task SetExpenseActual_WithinPlannedCost_KeepsApproved()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster(plannedQuantity: 3, plannedPrice: 100);
        await service.ApproveExpense(expenseId, null);

        // Факта не было; 2×100=200 меньше плана 3×100=300
        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(2, 100, new DateOnly(2026, 8, 20)));

        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Approved);
    }

    [Fact]
    public async Task SetExpenseActual_ExceedsPlannedCost_ResetsToProposed()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster(plannedQuantity: 1, plannedPrice: 100);
        await service.ApproveExpense(expenseId, null);

        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(5, 100, new DateOnly(2026, 8, 20)));

        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Proposed);
    }

    [Fact]
    public async Task SetExpenseActual_GrowsApprovedActual_ResetsToProposed()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster(plannedQuantity: 1, plannedPrice: 500);
        await service.ApproveExpense(expenseId, null);
        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(5, 200, new DateOnly(2026, 8, 20)));
        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Proposed);
        await service.ApproveExpense(expenseId, null);

        // 6×200=1200 больше предыдущего одобренного факта 5×200=1000
        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(6, 200, new DateOnly(2026, 8, 21)));

        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Proposed);
    }

    [Fact]
    public async Task SetExpenseActual_SameCostAsApprovedActual_ResetsToProposed()
    {
        var service = CreateService(mock.Master.UserId);
        var expenseId = await CreateExpenseAsMaster(plannedQuantity: 1, plannedPrice: 500);
        await service.ApproveExpense(expenseId, null);
        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(5, 200, new DateOnly(2026, 8, 20)));
        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Proposed);
        await service.ApproveExpense(expenseId, null);

        // Сравнение строгое «меньше»: равная сумма одобрение не сохраняет
        await service.SetExpenseActual(expenseId, new ExpenseActualRequest(5, 200, new DateOnly(2026, 8, 21)));

        expenses.ShouldHaveSingleItem().State.ShouldBe(ExpenseState.Proposed);
    }
}
