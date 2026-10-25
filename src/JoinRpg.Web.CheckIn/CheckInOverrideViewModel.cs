namespace JoinRpg.Web.CheckIn;

/// <summary>
/// Регистрация в обход: что администратор заявок пропускает и подтверждает.
/// </summary>
/// <param name="SkippedProblems">Препятствия так, как их видит мастер: «Паспорт — Поле не заполнено».</param>
/// <param name="BlockerKeys">Ключи тех же препятствий, уходят с формой и сверяются сервисом.</param>
/// <param name="FeeDue">Несданный взнос, остаётся долгом на заявке.</param>
/// <param name="MasterFullName">Кто подтверждает.</param>
public record CheckInOverrideViewModel(
    IReadOnlyList<string> SkippedProblems,
    IReadOnlyList<string> BlockerKeys,
    int FeeDue,
    string MasterFullName);
