using System.Globalization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces.Drp;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>Доменные операции, которыми пользуется применение, — от имени мастера проекта.</summary>
internal interface IManualFinanceDomain
{
    Task<PaymentTypeIdentification> CreateRestoredCashPaymentType(UserIdentification actingMaster, ProjectIdentification projectId, UserIdentification owner);

    Task<RestoredFinanceResult> RestoreFinanceOperations(
        UserIdentification actingMaster,
        ClaimIdentification claimId,
        IReadOnlyList<RestoredFinanceOperation> operations,
        IReadOnlyList<RestoredFinanceModeration> moderations);
}

/// <summary>
/// Применяет план одного проекта по проверенному CSV: создаёт подтверждённые типы «наличные», затем
/// восстанавливает по заявке. Ошибка по заявке не останавливает остальные — она попадает в отчёт.
/// В консоль — только количества и id.
/// </summary>
internal sealed class ManualFinanceApplier(IManualFinanceDomain domain, TextWriter output, TextWriter errorOutput)
{
    public async Task Apply(ProjectFinancePlan plan, IReadOnlyList<ManualFinanceCsvRow> csvRows, ApplyResults results, CancellationToken ct)
    {
        var projectText = plan.ProjectId.Value.ToString(CultureInfo.InvariantCulture);
        var rows = plan.Rows.Where(r => r.ClaimExists).ToList();
        foreach (var row in plan.Rows.Where(r => !r.ClaimExists))
        {
            results.Claims[row.ClaimId] = ApplyOutcome.Skipped("заявки нет в базе");
        }

        var decisions = ApplyBuilder.Match(plan, csvRows);
        foreach (var warning in decisions.Warnings)
        {
            output.WriteLine("  " + warning);
        }
        results.UnappliedCsvLines.AddRange(decisions.UnappliedChoices);

        if (plan.ActingMaster is not { } master)
        {
            // Тип «наличные» без мастера не создать, но для решения «есть ли что применять» хватит заглушки:
            // в домен этот вход не уходит.
            var placeholders = decisions.CashOwners.ToDictionary(o => o, _ => new PaymentTypeIdentification(plan.ProjectId, 0));
            var noMaster = ApplyOutcome.Error("в проекте нет действующего мастера с правом на деньги");
            var blocked = 0;
            foreach (var row in rows)
            {
                var input = ApplyBuilder.BuildClaim(row, decisions, placeholders);
                foreach (var (key, outcome) in input.NotPassed)
                {
                    results.Entries[(plan.ProjectId, key)] = outcome;
                }
                if (input.IsEmpty)
                {
                    results.Claims[row.ClaimId] = new ApplyOutcome("нечего применять");
                    continue;
                }
                foreach (var key in input.OperationKeys.Concat(input.ModerationKeys))
                {
                    results.Entries[(plan.ProjectId, key)] = noMaster;
                }
                results.Claims[row.ClaimId] = noMaster;
                blocked++;
            }
            if (blocked > 0)
            {
                errorOutput.WriteLine($"Проект {projectText}: нет действующего мастера с правом на деньги — не применено заявок: {blocked}.");
            }
            return;
        }

        var cashTypes = new Dictionary<UserIdentification, PaymentTypeIdentification>();
        foreach (var owner in decisions.CashOwners)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                cashTypes[owner] = await domain.CreateRestoredCashPaymentType(master, plan.ProjectId, owner);
                output.WriteLine($"Проект {projectText}: тип «наличные» мастера {owner.Value} — {cashTypes[owner].PaymentTypeId}.");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Операции на этот тип получат ошибку в отчёте (ApplyBuilder.BuildClaim).
                errorOutput.WriteLine($"Проект {projectText}: не удалось создать тип «наличные» мастера {owner.Value} ({e.GetType().Name}).");
            }
        }

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            var input = ApplyBuilder.BuildClaim(row, decisions, cashTypes);
            foreach (var (key, outcome) in input.NotPassed)
            {
                results.Entries[(plan.ProjectId, key)] = outcome;
            }
            if (input.IsEmpty)
            {
                results.Claims[row.ClaimId] = new ApplyOutcome("нечего применять");
                continue;
            }

            try
            {
                var result = await domain.RestoreFinanceOperations(master, row.ClaimId, input.Operations, input.Moderations);
                foreach (var (key, outcome) in ApplyBuilder.Outcomes(input, result))
                {
                    results.Entries[(plan.ProjectId, key)] = outcome;
                }
                results.Claims[row.ClaimId] = new ApplyOutcome(
                    $"применено: вставлено {result.Inserted}, уже было {result.AlreadyExisted}, пропущено из-за конфликта {result.Conflicts}");
                output.WriteLine($"  заявка {row.ClaimId.ClaimId}: вставлено {result.Inserted}, уже было {result.AlreadyExisted}, конфликтов {result.Conflicts}");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                var error = ApplyOutcome.Error(e.GetBaseException().Message);
                foreach (var key in input.OperationKeys.Concat(input.ModerationKeys))
                {
                    results.Entries[(plan.ProjectId, key)] = error;
                }
                results.Claims[row.ClaimId] = error;
                errorOutput.WriteLine($"  заявка {row.ClaimId.ClaimId}: ошибка ({e.GetType().Name}), подробности в отчёте.");
            }
        }
    }
}
