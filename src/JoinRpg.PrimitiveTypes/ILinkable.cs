namespace JoinRpg.PrimitiveTypes;

public interface ILinkable
{
    LinkType LinkType { get; }

    string? Identification { get; }
    int? ProjectId { get; }

    bool IsActive => true;
}

public interface ILinkableWithName : ILinkable
{
    string Name { get; }
}

public interface ILinkableClaim : ILinkable
{
    int ClaimId { get; }
}

public interface ILinkablePayment : ILinkableClaim
{
    int OperationId { get; }
}
