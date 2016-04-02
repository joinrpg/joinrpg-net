using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ
  public class ProjectFieldDropdownValue : IDeletableSubEntity, IProjectEntity
  {
    public int ProjectFieldDropdownValueId { get; set; }
    public int ProjectFieldId { get; set; }

    public virtual ProjectField ProjectField { get; set; }

    public int ProjectId { get; set; }

    public int Id => ProjectFieldDropdownValueId;

    public virtual Project Project { get; set; }

    bool IDeletableSubEntity.CanBePermanentlyDeleted => !WasEverUsed; 

    public bool IsActive { get; set; }

    public bool WasEverUsed { get; set; }

    public string Label { get; set; }

    public MarkdownString Description { get; set; }

    [CanBeNull]
    public virtual CharacterGroup CharacterGroup { get; set; }
    public int CharacterGroupId  { get; set; }
  }
}