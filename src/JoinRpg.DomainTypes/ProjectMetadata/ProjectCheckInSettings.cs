namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <param name="ShowCheckInBlockersToPlayers">
/// Показывать ли игроку, что мешает его регистрации на игре: незаполненные поля заявки, несданный взнос
/// и пометку, что проблема держит регистрацию. Проблемы профиля игрок видит всегда.
/// В БД — колонка <c>ProjectDetails.EnableCheckInModule</c> (историческое имя).
/// </param>
public record ProjectCheckInSettings(bool InProgress, bool AllowSecondRoles, bool ShowCheckInBlockersToPlayers);
