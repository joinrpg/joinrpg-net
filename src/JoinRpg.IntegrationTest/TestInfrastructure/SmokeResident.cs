namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>Жилец типа поселения в сид-проекте.</summary>
/// <param name="DisplayName">Отображаемое имя игрока: сайт показывает предпочитаемое имя.</param>
/// <param name="Phone">Телефон из профиля — его печатает отчёт по расселению.</param>
/// <param name="ClaimIsActive">
/// Активна ли заявка. Отклонённая заявка видна на странице типа поселения, но в отчёт по
/// расселению не попадает: <c>ClaimStatusSpec.Active</c> исключает отклонённые статусы.
/// </param>
/// <param name="IsPlaced">
/// Расселён ли жилец в комнату. Часть заявок сида остаётся нерасселённой — по этому флагу сценарии
/// знают, какое число должны показать счётчики занятости, не повторяя правило сида.
/// </param>
public sealed record SmokeResident(string DisplayName, string Phone, bool ClaimIsActive, bool IsPlaced);
