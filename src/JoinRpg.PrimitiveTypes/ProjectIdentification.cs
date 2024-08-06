namespace JoinRpg.PrimitiveTypes;

public record ProjectIdentification(int Value) : SingleValueType<int>(Value), ILinkable
{
    LinkType ILinkable.LinkType => LinkType.Project;

    string ILinkable.Identification => Value.ToString();

    int? ILinkable.ProjectId => Value;
}
