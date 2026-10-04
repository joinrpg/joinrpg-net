namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Пользователь, который писал в заявках проекта как мастер, но записи <c>ProjectAcl</c> у него нет (ADR019, §6).
/// </summary>
public record FormerMasterCandidate(ProjectIdentification ProjectId, UserIdentification UserId);
