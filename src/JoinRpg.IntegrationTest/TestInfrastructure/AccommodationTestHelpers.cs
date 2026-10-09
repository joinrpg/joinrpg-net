using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Помощники для сценариев поселения.
/// </summary>
public static class AccommodationTestHelpers
{
    /// <summary>
    /// Группы проживающих заявок — в том же порядке, что и <paramref name="claimIds"/>.
    /// </summary>
    /// <remarks>
    /// Читается из доменного снимка заявки (<c>CharacterClaimInfo.AccommodationGroupId</c>, ADR022),
    /// а не из результата операции: <c>IClaimService.SetAccommodationType</c> группу не возвращает —
    /// до сохранения её <c>Id</c> ещё не выдан базой.
    /// </remarks>
    /// <exception cref="InvalidOperationException">У заявки нет группы проживающих.</exception>
    public static async Task<IReadOnlyList<AccommodationRequestIdentification>> GetAccommodationGroupIdsAsync(
        IServiceProvider serviceProvider,
        IReadOnlyCollection<ClaimIdentification> claimIds)
    {
        var characters = await serviceProvider.GetRequiredService<ICharacterInfoRepository>()
            .GetCharacterInfosByClaims(claimIds);
        var claimsById = characters.SelectMany(character => character.Claims).ToDictionary(claim => claim.ClaimId);

        return [.. claimIds.Select(claimId =>
            claimsById[claimId].AccommodationGroupId.AsAccommodationRequestId()
                ?? throw new InvalidOperationException($"У заявки {claimId} нет группы проживающих"))];
    }
}
