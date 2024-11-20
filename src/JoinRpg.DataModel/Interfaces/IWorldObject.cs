using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel;

public interface IWorldObject : IProjectEntity, ILinkable
{
    IEnumerable<CharacterGroup> ParentGroups { get; }
    string Name { get; }
    bool IsPublic { get; }

    MarkdownString Description { get; }

    new int ProjectId => ((IProjectEntity)this).ProjectId;
}
