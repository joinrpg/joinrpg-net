namespace JoinRpg.Web.Claims.Finance;

/// <summary>
/// Ручное назначение размера базового взноса заявки.
/// </summary>
public interface IChangeFeeClient
{
    Task ChangeFee(ClaimIdentification claimId, ChangeFeeRequest request);
}

/// <param name="FeeValue">Новый размер базового взноса</param>
public record ChangeFeeRequest(int FeeValue);
