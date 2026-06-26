namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

public record class ClaimBalance(int FeePaid, int TotalFee)
{
    public int FeeDue { get; } = TotalFee - FeePaid;
}
