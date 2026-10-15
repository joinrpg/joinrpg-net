using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ManualFinance;

namespace JoinRpg.Tools.RestoreManualFinance.Test;

/// <summary>Строки плана для тестов — без планировщика: сборку входа и отчёт проверяем на готовом плане.</summary>
internal static class PlanFactory
{
    public static readonly ProjectIdentification Project = new(1600);
    public static readonly ClaimIdentification Claim = new(Project, 100);
    public static readonly ClaimIdentification OtherClaim = new(Project, 101);
    public static readonly UserIdentification Master = new(10);
    public static readonly UserIdentification Player = new(20);
    public static readonly DateTimeOffset At = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    public static PaymentTypeForRestore CashType(int id = 7, UserIdentification? owner = null)
        => new(new PaymentTypeIdentification(Project, id), PaymentTypeKind.Cash, owner ?? Master, IsActive: true, "Наличные мастеру");

    public static PaymentTypeForRestore OnlineType(int id = 8) => new(new PaymentTypeIdentification(Project, id), PaymentTypeKind.Online, Master, IsActive: true, "Онлайн");

    public static PaymentTypeSuggestion Suggested(int id) => new(id, CreateCashFor: null, PaymentTypeGuess.Initiator, Candidates: []);

    public static PaymentTypeSuggestion CreateCash(UserIdentification owner) => new(PaymentTypeId: null, owner, PaymentTypeGuess.CreateCash, Candidates: []);

    public static PaymentTypeSuggestion NotFound() => new(PaymentTypeId: null, CreateCashFor: null, PaymentTypeGuess.NotFound, Candidates: []);

    public static FinanceOperationToRestore Operation(
        string key,
        int? id = null,
        bool byPlayer = false,
        FinanceOperationType type = FinanceOperationType.Submit,
        int? money = 1500,
        DateTimeOffset? at = null,
        FinanceOperationState? state = null,
        DateTimeOffset? moderatedAt = null,
        PaymentTypeSuggestion? suggestion = null,
        FinanceRestoreStatus status = FinanceRestoreStatus.Restore)
        => new(key, id, byPlayer ? Player : Master, byPlayer, type, money, at ?? At, "Текст операции",
            type == FinanceOperationType.Submit ? CommentExtraAction.PaidFee : CommentExtraAction.RefundFee,
            state ?? (byPlayer ? FinanceOperationState.Proposed : FinanceOperationState.Approved),
            moderatedAt,
            suggestion ?? Suggested(7),
            status);

    public static FinanceModerationToRestore Moderation(
        string key,
        FinanceModerationLink link,
        int? id = null,
        bool approved = true,
        DateTimeOffset? at = null,
        string? targetKey = null,
        int? targetId = null,
        FinanceRestoreStatus status = FinanceRestoreStatus.Restore)
        => new(key, id, Master, approved, at ?? At.AddDays(1), "Текст решения", link, targetKey, targetId, status);

    public static FinanceRestorePlanRow Row(
        IReadOnlyList<FinanceOperationToRestore>? operations = null,
        IReadOnlyList<FinanceModerationToRestore>? moderations = null,
        ClaimIdentification? claim = null,
        bool exists = true,
        IReadOnlyCollection<FinanceRestoreWarning>? warnings = null,
        int transfers = 0)
        => new(claim ?? Claim, exists, operations ?? [], moderations ?? [], SkippedForMissingClaim: 0, transfers,
            PreferentialRequests: 0, OnlinePayments: 0, Unparsed: 0, warnings ?? []);

    public static ProjectFinancePlan Plan(params FinanceRestorePlanRow[] rows)
        => new(Project, rows, [CashType(), OnlineType()], Master);

    public static ManualFinanceCsvRow Csv(string key, string? paymentType = null, int line = 2, ProjectIdentification? project = null)
    {
        PaymentTypeChoice.TryParse(paymentType, out var choice).ShouldBeTrue();
        return new ManualFinanceCsvRow(line, project ?? Project, key, choice);
    }
}
