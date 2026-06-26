using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DataModel;

public interface IWorldObject : IProjectEntity, ILinkable
{
    string Name { get; }
    bool IsPublic { get; }

    MarkdownDbValue Description { get; }

    new int ProjectId => ((IProjectEntity)this).ProjectId;
}
