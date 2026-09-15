using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.DataModel.Extensions;

/// <summary>
/// Извлечение идентификаторов связанных сущностей из заявки. Живёт в <c>JoinRpg.DataModel</c>,
/// а не в <c>JoinRpg.Domain</c>, потому что нужно и слою доступа к данным: write-репозиторий
/// агрегата персонажа резолвит персонажа по заявке (ADR014), а <c>JoinRpg.Dal.Impl</c> на
/// <c>JoinRpg.Domain</c> не ссылается и ссылаться не должен.
/// </summary>
public static class ClaimExtensions
{
    /// <summary>Идентификатор персонажа, на которого подана заявка.</summary>
    public static CharacterIdentification GetCharacterId(this Claim claim) => new(claim.ProjectId, claim.CharacterId);

    /// <summary>Идентификатор игрока, подавшего заявку.</summary>
    public static UserIdentification GetPlayerId(this Claim claim) => new(claim.PlayerUserId);
}
