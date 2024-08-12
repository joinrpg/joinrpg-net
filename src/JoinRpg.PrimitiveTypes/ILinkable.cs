namespace JoinRpg.PrimitiveTypes;

public interface ILinkable
{
    LinkType LinkType { get; }

    string? Identification { get; }
    int? ProjectId { get; }
}

public interface ILinkableClaim : ILinkable
{
    int ClaimId { get; }
}

public interface ILinkablePayment : ILinkableClaim
{
    int OperationId { get; }
}
