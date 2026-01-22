
namespace JoinRpg.PrimitiveTypes;

public record GroupTarget(CharacterGroupIdentification CharacterGroupId, string Name) : ILinkableWithName
{
    LinkType ILinkable.LinkType => LinkType.ResultCharacterGroup;

    string? ILinkable.Identification => CharacterGroupId.CharacterGroupId.ToString();

    int? ILinkable.ProjectId => CharacterGroupId.ProjectId;
}

public record CharacterTarget(CharacterIdentification CharacterId, string Name) : ILinkableWithName
{
    LinkType ILinkable.LinkType => LinkType.ResultCharacter;

    string? ILinkable.Identification => CharacterId.Id.ToString();

    int? ILinkable.ProjectId => CharacterId.ProjectId;
}

public record TargetsInfo(IReadOnlyCollection<CharacterTarget> CharacterTargets, IReadOnlyCollection<GroupTarget> GroupTargets)
{
    public bool HasIntersections(TargetsInfo other) => CharacterTargets.Intersect(other.CharacterTargets).Any() || GroupTargets.Intersect(other.GroupTargets).Any();

    public IEnumerable<ILinkableWithName> AllLinks => CharacterTargets.Cast<ILinkableWithName>().Union(GroupTargets);

    public bool Any() => CharacterTargets.Count != 0 || GroupTargets.Count != 0;
}

public static class TargetsExtensions
{
    public static TargetsInfo UnionAll(this IEnumerable<TargetsInfo> enumerable)
    => new TargetsInfo([.. enumerable.SelectMany(x => x.CharacterTargets).Distinct()], [.. enumerable.SelectMany(x => x.GroupTargets).Distinct()]);
}
