namespace JoinRpg.PrimitiveTypes;

public interface ILinkable
{
    LinkType LinkType { get; }

    string Identification { get; }
    int? ProjectId { get; }
}
