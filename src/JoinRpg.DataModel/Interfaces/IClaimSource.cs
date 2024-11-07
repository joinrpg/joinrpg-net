namespace JoinRpg.DataModel;

public interface IClaimSource : IWorldObject
{
    IEnumerable<Claim> Claims { get; }
    User? ResponsibleMasterUser { get; }
    bool IsRoot { get; }
}
