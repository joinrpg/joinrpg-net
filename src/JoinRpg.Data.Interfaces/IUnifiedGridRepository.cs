using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;
public interface IUnifiedGridRepository
{
    Task<IReadOnlyCollection<UgDto>> GetByGroups(ProjectIdentification projectId, UgStatusSpec spec, IReadOnlyCollection<CharacterGroupIdentification> groups);
}

public record class UgDto(
    CharacterTypeInfo CharacterTypeInfo,
    string CharacterName,
    UserIdentification? ApprovedClaimUserId,
    bool IsActive,
    bool HasActiveClaims,
    CharacterIdentification CharacterId,
    IReadOnlyCollection<UgClaim> Claims
    );

public record class UgClaim(
    Claim Claim,
    int FeePaid);


public enum UgStatusSpec
{
    /// <summary>
    /// Активные персонажи + активные заявки на них
    /// </summary>
    Active,
    /// <summary>
    /// Активные персонажи без утвержденных заявок + активные заявки на них
    /// </summary>
    Vacant,
    /// <summary>
    /// Активные непринятые заявки + персонажи к ним
    /// </summary>
    Discussion,
    /// <summary>
    /// (Удаленные персонажи + все заявки на них) + (активные персонажи с отклоненными и отозванными заявками + отклоненные/отозванные заявки)
    /// </summary>
    Archive,
}
