using System.Collections.Generic;

namespace JoinRpg.DataModel
{
  public interface IWorldObject : IProjectSubEntity
  {
    ICollection<CharacterGroup> ParentGroups { get;  }
    string Name { get; }
    bool IsPublic { get;  }

    bool IsActive { get; }
    MarkdownString Description { get; }
  }
}