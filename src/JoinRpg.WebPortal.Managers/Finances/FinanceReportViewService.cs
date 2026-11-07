using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.WebPortal.Managers.Finances;

/// <summary>
/// Сводка по финансам проекта: страница «Финансы» (MoneySummary) и публичная CSV-выгрузка
/// балансов мастеров по токену (SummaryByMaster). Общая часть обоих сценариев — операции,
/// переводы и мастера с деньгами — грузится здесь один раз.
/// </summary>
public class FinanceReportViewService(
    IProjectRepository projectRepository,
    IFinanceReportRepository financeReportRepository,
    IExpenseRepository expenseRepository,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor)
{
    public async Task<MoneyInfoTotalViewModel?> GetMoneySummaryViewModel(ProjectIdentification projectId)
    {
        var context = await LoadFinanceSummaryContext(projectId);
        if (context is null)
        {
            return null;
        }

        var payments = context.ProjectInfo.ProjectFinanceSettings.PaymentTypes
            .Select(pt => new PaymentTypeSummaryViewModel(pt, context.Operations))
            .Where(m => m.Total != 0)
            .OrderByDescending(m => m.Total)
            .ToArray();

        return new MoneyInfoTotalViewModel(
            context.ProjectInfo,
            context.Transfers,
            context.Operations,
            context.Expenses,
            payments,
            currentUserAccessor,
            context.Masters);
    }

    public async Task<MasterBalancesReport?> GetMasterBalancesReport(ProjectIdentification projectId)
    {
        var context = await LoadFinanceSummaryContext(projectId);
        if (context is null)
        {
            return null;
        }

        return new MasterBalancesReport(
            context.ProjectInfo.ProjectName,
            MasterBalanceBuilder.ToMasterBalanceViewModels(context.Masters, context.Operations, context.Transfers, context.Expenses, context.ProjectInfo));
    }

    private async Task<FinanceSummaryContext?> LoadFinanceSummaryContext(ProjectIdentification projectId)
    {
        var project = await projectRepository.GetProjectWithFinances(projectId);
        if (project is null)
        {
            return null;
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var transfers = await financeReportRepository.GetAllMoneyTransfers(projectId);
        var operations = project.FinanceOperations.ToArray();
        var expenses = await expenseRepository.GetExpenses(projectId);

        var masterIds = MasterBalanceBuilder.GetMasterIds(operations, transfers, expenses, projectInfo);
        var masters = await userRepository.GetRequiredUserInfos(masterIds);

        return new FinanceSummaryContext(project, projectInfo, operations, transfers, expenses, masters);
    }

    private sealed record FinanceSummaryContext(
        Project Project,
        ProjectInfo ProjectInfo,
        IReadOnlyCollection<FinanceOperation> Operations,
        IReadOnlyCollection<MoneyTransfer> Transfers,
        IReadOnlyCollection<ExpenseInfo> Expenses,
        IReadOnlyCollection<UserInfo> Masters);
}

/// <summary>Балансы мастеров для выгрузки: файл называется именем проекта.</summary>
public sealed record MasterBalancesReport(
    string ProjectName,
    IReadOnlyCollection<MasterBalanceViewModel> Balances);
