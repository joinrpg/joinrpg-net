namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Роль мастера в проекте — свободный текст: «Главный мастер», «Мастер по боёвке» (ADR019, §4).
/// Обязательна, поэтому пустую роль не собрать в принципе. Длина — как у колонки ProjectAcls.Role.
/// </summary>
[TypedStringValue(MaxLength = 100)]
public partial record MasterRoleTitle(string Value);
