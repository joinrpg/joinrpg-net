using System.Collections.Generic;

namespace JoinRpg.DataModel
{
  public interface IWorldObject : IProjectEntity
  {
    ICollection<CharacterGroup> ParentGroups { get;  }
    string Name { get; }
    bool IsPublic { get;  }

    bool IsActive { get; }
    MarkdownString Description { get; }
  }
}