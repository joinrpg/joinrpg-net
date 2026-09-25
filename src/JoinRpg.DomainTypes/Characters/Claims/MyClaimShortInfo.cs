using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Заявка текущего пользователя в минимальном объеме — для списков и меню
/// </summary>
public record MyClaimShortInfo(ClaimIdentification ClaimId, ProjectName ProjectName, string CharacterName);
