using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Services.Impl.Finances;

internal class ExpenseService(
    IUnitOfWork unitOfWork,
    IProjectMetadataRepository metadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    IExpenseRepository expenseRepository,
    ExpenseCategoryResolver categoryResolver,
    ILogger<ExpenseService> logger) : IExpenseService
{
    public async Task<ExpenseIdentification> CreateExpense(ProjectIdentification projectId, CreateExpenseRequest request)
    {
        var projectInfo = await RequestMasterAccess(projectId);
        _ = projectInfo.RequestMasterAccess(request.MasterId);
        ValidateActualPair(request.ActualQuantity, request.ActualPrice);

        var category = await categoryResolver.ResolveCategoryPath(projectInfo.ProjectId, request.CategoryPath);

        var now = DateTime.UtcNow;
        var expense = new Expense
        {
            ProjectId = projectInfo.ProjectId.Value,
            Name = request.Name,
            Description = new MarkdownDbValue(request.Description?.Value),
            MasterId = request.MasterId.Value,
            CategoryId = category.LeafId,
            CategoryPathIds = category.PathIds,
            PlannedQuantity = request.PlannedQuantity,
            PlannedPrice = request.PlannedPrice,
            ActualQuantity = request.ActualQuantity,
            ActualPrice = request.ActualPrice,
            SpentAt = request.SpentAt?.ToDateTime(TimeOnly.MinValue),
            State = ExpenseState.Proposed,
        };
        EntityAudit.MarkCreated(expense, now, currentUserAccessor.UserId);
        _ = unitOfWork.GetDbSet<Expense>().Add(expense);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation(
            "Создан расход {expenseId} в проекте {projectId}: {name}, мастер {masterId}",
            expense.ExpenseId,
            projectId.Value,
            expense.Name,
            expense.MasterId);
        return new ExpenseIdentification(projectId, expense.ExpenseId);
    }

    public async Task EditExpense(ExpenseIdentification expenseId, EditExpenseRequest request)
    {
        var projectInfo = await RequestMasterAccess(expenseId.ProjectId);
        var expense = await LoadExpense(expenseId);
        RequestOwnExpenseAccess(projectInfo, expense);
        ValidateActualPair(request.ActualQuantity, request.ActualPrice);

        if (projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney))
        {
            _ = projectInfo.RequestMasterAccess(request.MasterId);
        }
        else if (request.MasterId != new UserIdentification(expense.MasterId))
        {
            // Без прав на деньги расход нельзя переложить на другого мастера.
            throw new NoAccessToProjectException(projectInfo, currentUserAccessor.UserIdentificationOrDefault?.Value, Permission.CanManageMoney);
        }

        var category = await categoryResolver.ResolveCategoryPath(expenseId.ProjectId, request.CategoryPath);

        var previousPlannedCost = ExpenseCost.Planned(expense.PlannedQuantity, expense.PlannedPrice);
        var newPlannedCost = ExpenseCost.Planned(request.PlannedQuantity, request.PlannedPrice);
        // Правила сброса одобрения (см. SetExpenseActual) считаем до изменения расхода.
        // Факт-правило — только при реальной смене факта: та же пара, пересланная формой,
        // одобрение не сбрасывает; сверяем факт с результирующим (новым) планом.
        var planKeepsApproval = newPlannedCost <= previousPlannedCost;
        var actualChanged = expense.ActualQuantity != request.ActualQuantity || expense.ActualPrice != request.ActualPrice;
        var actualKeepsApproval = !actualChanged
            || request.ActualQuantity is null
            || ActualChangeKeepsApproval(
                newPlannedCost,
                ExpenseCost.Actual(expense.ActualQuantity, expense.ActualPrice),
                ExpenseCost.Planned(request.ActualQuantity.Value, request.ActualPrice.Value));

        expense.Name = request.Name;
        expense.Description = new MarkdownDbValue(request.Description?.Value);
        expense.MasterId = request.MasterId.Value;
        expense.CategoryId = category.LeafId;
        expense.CategoryPathIds = category.PathIds;
        expense.PlannedQuantity = request.PlannedQuantity;
        expense.PlannedPrice = request.PlannedPrice;
        expense.ActualQuantity = request.ActualQuantity;
        expense.ActualPrice = request.ActualPrice;
        expense.SpentAt = request.SpentAt?.ToDateTime(TimeOnly.MinValue);
        // Одобрение сохраняем, только если не вырос ни план, ни факт (правила #6180):
        // уменьшение или та же сумма денежную сторону не меняют, иначе согласование требуется заново.
        expense.State = expense.State == ExpenseState.Approved && planKeepsApproval && actualKeepsApproval
            ? ExpenseState.Approved
            : ExpenseState.Proposed;
        EntityAudit.MarkChanged(expense, DateTime.UtcNow, currentUserAccessor.UserId);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation(
            "Изменён расход {expenseId} в проекте {projectId}: {name}",
            expense.ExpenseId,
            expenseId.ProjectId.Value,
            expense.Name);
    }

    public async Task SetExpenseActual(ExpenseIdentification expenseId, ExpenseActualRequest request)
    {
        var projectInfo = await RequestMasterAccess(expenseId.ProjectId);
        var expense = await LoadExpense(expenseId);
        RequestOwnExpenseAccess(projectInfo, expense);

        var plannedCost = ExpenseCost.Planned(expense.PlannedQuantity, expense.PlannedPrice);
        var previousActual = ExpenseCost.Actual(expense.ActualQuantity, expense.ActualPrice);
        var newCost = ExpenseCost.Planned(request.ActualQuantity, request.ActualPrice);

        expense.ActualQuantity = request.ActualQuantity;
        expense.ActualPrice = request.ActualPrice;
        expense.SpentAt = request.SpentAt.ToDateTime(TimeOnly.MinValue);
        // Одобрение сохраняем, только если факт не вырос: меньше предыдущего одобренного
        // факта или в пределах плана. Иначе согласование требуется заново.
        expense.State = expense.State == ExpenseState.Approved
            && ActualChangeKeepsApproval(plannedCost, previousActual, newCost)
            ? ExpenseState.Approved
            : ExpenseState.Proposed;
        EntityAudit.MarkChanged(expense, DateTime.UtcNow, currentUserAccessor.UserId);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation(
            "Записан факт расхода {expenseId} в проекте {projectId}: {actualQuantity} шт. по {actualPrice}",
            expense.ExpenseId,
            expenseId.ProjectId.Value,
            expense.ActualQuantity,
            expense.ActualPrice);
    }

    /// <summary>Факт задаётся только парой «количество + цена»: оба поля или ни одного.</summary>
    private static void ValidateActualPair(int? actualQuantity, decimal? actualPrice)
    {
        if ((actualQuantity is null) != (actualPrice is null))
        {
            throw new JoinValidationException("TODO[Localize]: факт расхода задаётся парой — укажите и количество, и цену, или ни одного из них");
        }
    }

    /// <summary>Правило #6180 для факта: одобрение сохраняется, только если фактическая стоимость
    /// не выросла — меньше прежнего факта или в пределах плана (при совмещённой правке —
    /// результирующего нового плана). Равная прежнему факту сумма одобрение не сохраняет,
    /// когда она не ниже плана. Применяется только если факт реально изменился.</summary>
    private static bool ActualChangeKeepsApproval(decimal plannedCost, decimal? previousActualCost, decimal newActualCost)
        => (previousActualCost is { } previous && newActualCost < previous) || newActualCost < plannedCost;

    /// <summary>Мастер без <see cref="Permission.CanManageMoney"/> может править только свои
    /// расходы (понёс сам) или созданные собой.</summary>
    private void RequestOwnExpenseAccess(ProjectInfo projectInfo, Expense expense)
    {
        if (projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney))
        {
            return;
        }

        var userId = currentUserAccessor.UserIdentificationOrDefault?.Value;
        if (userId is null || (expense.MasterId != userId.Value && expense.CreatedById != userId.Value))
        {
            throw new NoAccessToProjectException(projectInfo, userId, Permission.CanManageMoney);
        }
    }

    public async Task ApproveExpense(ExpenseIdentification expenseId, MarkdownString? comment)
        => await ChangeState(expenseId, ExpenseState.Approved, comment);

    public async Task RejectExpense(ExpenseIdentification expenseId, MarkdownString? comment)
        => await ChangeState(expenseId, ExpenseState.Rejected, comment);

    public async Task<IReadOnlyCollection<string>> LoadCategoryPathSuggestions(ProjectIdentification projectId)
    {
        _ = await RequestMasterAccess(projectId);
        var allCategories = await expenseRepository.GetCategories(projectId);
        var categoriesById = allCategories.ToDictionary(c => c.CategoryId.ExpenseCategoryId);

        // Сначала категории с не отклонёнными расходами, потом все их предки; исключаем дубли
        // по id — категория может попасть и самостоятельно, и как родитель другой попавшей.
        var suitableCategoryIds = new HashSet<int>();
        foreach (var category in allCategories.Where(c => c.HasActiveDirectExpenses))
        {
            for (ExpenseCategoryInfo? current = category; current is not null;
                current = current.ParentCategoryId is { } parent ? categoriesById[parent.ExpenseCategoryId] : null)
            {
                _ = suitableCategoryIds.Add(current.CategoryId.ExpenseCategoryId);
            }
        }

        return suitableCategoryIds
            .Select(id => categoriesById[id].FullPath)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .OrderBy(path => path, StringComparer.InvariantCulture)
            .ToArray();
    }

    private async Task ChangeState(ExpenseIdentification expenseId, ExpenseState state, MarkdownString? comment)
    {
        _ = await RequestMasterAccess(expenseId.ProjectId, Permission.CanManageMoney);
        var expense = await LoadExpense(expenseId);
        var now = DateTime.UtcNow;

        expense.State = state;
        EntityAudit.MarkChanged(expense, now, currentUserAccessor.UserId);
        _ = unitOfWork.GetDbSet<ExpenseApproval>().Add(new ExpenseApproval
        {
            ExpenseId = expense.ExpenseId,
            State = state,
            ApprovedById = currentUserAccessor.UserId,
            Timestamp = now,
            Comment = new MarkdownDbValue(comment?.Value),
        });
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation(
            "Расход {expenseId} в проекте {projectId} переведён в состояние {state} пользователем {userId}",
            expense.ExpenseId,
            expenseId.ProjectId.Value,
            state,
            currentUserAccessor.UserId);
    }

    private async Task<ProjectInfo> RequestMasterAccess(ProjectIdentification projectId, Permission permission = Permission.None)
    {
        var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, permission);
        return projectInfo;
    }

    private async Task<Expense> LoadExpense(ExpenseIdentification expenseId)
    {
        var expense = await unitOfWork.GetDbSet<Expense>()
            .SingleOrDefaultAsync(e => e.ExpenseId == expenseId.ExpenseId && e.ProjectId == expenseId.ProjectId.Value);
        return expense ?? throw new JoinRpgEntityNotFoundException([expenseId.ExpenseId], nameof(Expense));
    }
}
