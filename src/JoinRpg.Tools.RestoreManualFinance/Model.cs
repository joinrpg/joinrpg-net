using System.Globalization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.Services.Drp.ManualFinance;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>План одного заблокированного проекта и то, что нужно для отчёта и применения.</summary>
/// <param name="Rows">План по заявкам (<see cref="FinanceRestorePlanner"/>).</param>
/// <param name="PaymentTypes">Все типы оплаты проекта — для проверки CSV и названий в отчёте.</param>
/// <param name="ActingMaster">
/// От чьего имени применять (<see cref="FinanceSourceBuilder.ChooseActingMaster"/>); null — в проекте нет
/// действующего мастера с правом на деньги, применить нельзя.
/// </param>
internal record ProjectFinancePlan(
    ProjectIdentification ProjectId,
    IReadOnlyList<FinanceRestorePlanRow> Rows,
    IReadOnlyList<PaymentTypeForRestore> PaymentTypes,
    UserIdentification? ActingMaster)
{
    /// <summary>Ключи всех операций и модераций плана.</summary>
    public IEnumerable<string> Keys => Rows.SelectMany(r => r.Operations.Select(o => o.Key).Concat(r.Moderations.Select(m => m.Key)));
}

/// <summary>
/// Значение колонки «Тип оплаты»: существующий тип проекта или «создать наличные на мастера» — ровно одно.
/// </summary>
internal record PaymentTypeChoice(int? PaymentTypeId, UserIdentification? CashFor)
{
    public const string CashPrefix = "cash:";

    public static PaymentTypeChoice Existing(int paymentTypeId) => new(paymentTypeId, CashFor: null);

    public static PaymentTypeChoice Cash(UserIdentification owner) => new(PaymentTypeId: null, owner);

    public string Format()
        => PaymentTypeId is int id
            ? id.ToString(CultureInfo.InvariantCulture)
            : CashPrefix + CashFor!.Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Пусто — null; иначе <c>&lt;id&gt;</c> или <c>cash:&lt;userId&gt;</c>.</summary>
    /// <returns>false — значение не разобрано.</returns>
    public static bool TryParse(string? value, out PaymentTypeChoice? choice)
    {
        choice = null;
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
        {
            return true;
        }
        if (text.StartsWith(CashPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (TryParsePositive(text[CashPrefix.Length..].Trim(), out var userId))
            {
                choice = Cash(new UserIdentification(userId));
                return true;
            }
            return false;
        }
        if (TryParsePositive(text, out var paymentTypeId))
        {
            choice = Existing(paymentTypeId);
            return true;
        }
        return false;
    }

    private static bool TryParsePositive(string text, out int value)
        => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;
}

/// <summary>Чем кончилась строка плана при применении.</summary>
/// <param name="IsError">Ошибка — итоговый код возврата 1.</param>
internal record ApplyOutcome(string Text, bool IsError = false)
{
    public static ApplyOutcome Error(string text) => new("ошибка: " + text, IsError: true);

    public static ApplyOutcome Skipped(string text) => new("пропущено: " + text);
}

/// <summary>Результаты применения для отчёта: по строкам плана (проект, ключ) и по заявкам. Пусто в dry-run.</summary>
internal sealed class ApplyResults
{
    public Dictionary<(ProjectIdentification ProjectId, string Key), ApplyOutcome> Entries { get; } = [];

    public Dictionary<ClaimIdentification, ApplyOutcome> Claims { get; } = [];

    /// <summary>
    /// Номера строк CSV с непустым «Тип оплаты», которые не применились: проект не заблокирован или ключа нет
    /// в плане. Выбор человека потерялся — это ошибка прогона.
    /// </summary>
    public List<int> UnappliedCsvLines { get; } = [];

    public bool HasErrors => UnappliedCsvLines.Count > 0 || Entries.Values.Concat(Claims.Values).Any(o => o.IsError);
}
