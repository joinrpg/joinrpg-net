namespace JoinRpg.Services.Interfaces;

/// <param name="MasterId">Мастер, понёсший расход (по умолчанию подставляет UI — текущий пользователь)</param>
/// <param name="CategoryPath">Полный путь категории через «\», «Аренда базы\Жилые корпуса»; сервис делает find-or-create</param>
/// <param name="SpentAt">Дата расхода; может быть указана и у планового расхода (без факта)</param>
/// <param name="Description">Описание (markdown)</param>
/// <param name="ActualQuantity">Фактическое количество; задаётся только в паре с <paramref name="ActualPrice"/></param>
/// <param name="ActualPrice">Фактическая цена; задаётся только в паре с <paramref name="ActualQuantity"/></param>
public record CreateExpenseRequest(
    UserIdentification MasterId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    int PlannedPrice,
    DateOnly? SpentAt,
    MarkdownString? Description,
    int? ActualQuantity = null,
    int? ActualPrice = null);

/// <param name="SpentAt">Дата расхода; может быть указана и у планового расхода (без факта)</param>
/// <param name="Description">Описание (markdown)</param>
/// <param name="ActualQuantity">Фактическое количество; задаётся только в паре с <paramref name="ActualPrice"/></param>
/// <param name="ActualPrice">Фактическая цена; задаётся только в паре с <paramref name="ActualQuantity"/></param>
public record EditExpenseRequest(
    UserIdentification MasterId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    int PlannedPrice,
    DateOnly? SpentAt,
    MarkdownString? Description,
    int? ActualQuantity = null,
    int? ActualPrice = null);

public record ExpenseActualRequest(
    int ActualQuantity,
    int ActualPrice,
    DateOnly SpentAt);
