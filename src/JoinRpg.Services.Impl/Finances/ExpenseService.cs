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

        var category = await categoryResolver.ResolveCategoryPath(projectInfo.ProjectId, request.CategoryPath);

        var now = DateTime.UtcNow;
        var expense = new Expense
        {
            Name = request.Name,
            Description = new MarkdownDbValue(request.Description?.Value),
            MasterId = request.MasterId.Value,
            CategoryId = category.LeafId,
            CategoryPathIds = category.PathIds,
            PlannedQuantity = request.PlannedQuantity,
            PlannedPrice = request.PlannedPrice,
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

        expense.Name = request.Name;
        expense.Description = new MarkdownDbValue(request.Description?.Value);
        expense.MasterId = request.MasterId.Value;
        expense.CategoryId = category.LeafId;
        expense.CategoryPathIds = category.PathIds;
        expense.PlannedQuantity = request.PlannedQuantity;
        expense.PlannedPrice = request.PlannedPrice;
        expense.State = ExpenseState.Proposed;
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

        var plannedCost = expense.PlannedQuantity * expense.PlannedPrice;
        var previousActual = expense.ActualQuantity is { } quantity && expense.ActualPrice is { } price ? quantity * price : (int?)null;
        var newCost = request.ActualQuantity * request.ActualPrice;

        expense.ActualQuantity = request.ActualQuantity;
        expense.ActualPrice = request.ActualPrice;
        expense.SpentAt = request.SpentAt.ToDateTime(TimeOnly.MinValue);
        // Одобрение сохраняем, только если факт не вырос: меньше предыдущего одобренного
        // факта или в пределах плана. Иначе согласование требуется заново.
        expense.State = expense.State == ExpenseState.Approved
            && ((previousActual is { } prev && newCost < prev) || newCost < plannedCost)
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
