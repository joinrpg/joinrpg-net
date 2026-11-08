namespace JoinRpg.Services.Interfaces;

/// <param name="MasterId">Мастер, понёсший расход (по умолчанию подставляет UI — текущий пользователь)</param>
/// <param name="CategoryPath">Полный путь категории через «\», «Аренда базы\Жилые корпуса»; сервис делает find-or-create</param>
/// <param name="SpentAt">Дата расхода; может быть указана и у планового расхода (без факта)</param>
/// <param name="Description">Описание (markdown)</param>
public record CreateExpenseRequest(
    UserIdentification MasterId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    int PlannedPrice,
    DateOnly? SpentAt,
    MarkdownString? Description);

/// <param name="SpentAt">Дата расхода; может быть указана и у планового расхода (без факта)</param>
/// <param name="Description">Описание (markdown)</param>
public record EditExpenseRequest(
    UserIdentification MasterId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    int PlannedPrice,
    DateOnly? SpentAt,
    MarkdownString? Description);

public record ExpenseActualRequest(
    int ActualQuantity,
    int ActualPrice,
    DateOnly SpentAt);
