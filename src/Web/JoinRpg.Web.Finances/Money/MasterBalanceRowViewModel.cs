using JoinRpg.Common.WebComponents;

namespace JoinRpg.Web.Finances.Money;

/// <summary>Строка сводки балансов мастеров по одному мастеру. Итоговую строку «Итого»
/// компонент строит сам из сумм по колонкам — вызывающая сторона её не создаёт.</summary>
/// <param name="DetailsLink">Готовый URL страницы баланса мастера (ByMaster). Строкой, а не
/// локатором: компонент рендерится статически из MVC-вью, где URL строит Url.Action.</param>
public record MasterBalanceRowViewModel(
    UserLinkViewModel? MasterLink,
    decimal Total,
    decimal FeeBalance,
    decimal ReceiveBalance,
    decimal SendBalance,
    decimal ExpensesBalance,
    decimal PlannedExpenses,
    string? DetailsLink = null)
{
    /// <summary>Строка «Итого» по всем мастерам; выставляет только сам компонент.</summary>
    internal bool IsTotal { get; init; }
}
