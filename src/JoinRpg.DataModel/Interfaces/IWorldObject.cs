namespace JoinRpg.DataModel;

public interface IWorldObject : IProjectEntity, ILinkable
{
    string Name { get; }
    bool IsPublic { get; }

    MarkdownString Description { get; }

    new int ProjectId => ((IProjectEntity)this).ProjectId;
}
