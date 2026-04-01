using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId(ShortName = "Project")]
public partial record ProjectIdentification(int Value) : SingleValueType<int>(Value), ILinkable, IProjectEntityId
{
    LinkType ILinkable.LinkType => LinkType.Project;

    string ILinkable.Identification => Value.ToString();

    int? ILinkable.ProjectId => Value;

    ProjectIdentification IProjectEntityId.ProjectId => this;

    int IProjectEntityId.Id => Value;
}
