namespace JoinRpg.Services.Interfaces;

/// <param name="MasterId">Мастер, понёсший расход (по умолчанию подставляет UI — текущий пользователь)</param>
/// <param name="CategoryPath">Полный путь категории через «\», «Аренда базы\Жилые корпуса»; сервис делает find-or-create</param>
/// <param name="Description">Описание (markdown)</param>
public record CreateExpenseRequest(
    UserIdentification MasterId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    int PlannedPrice,
    MarkdownString? Description);

/// <param name="Description">Описание (markdown)</param>
public record EditExpenseRequest(
    UserIdentification MasterId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    int PlannedPrice,
    MarkdownString? Description);

public record ExpenseActualRequest(
    int ActualQuantity,
    int ActualPrice,
    DateOnly SpentAt);
