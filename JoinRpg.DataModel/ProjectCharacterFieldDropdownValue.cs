namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ
  public class ProjectCharacterFieldDropdownValue : IDeletableSubEntity, IProjectEntity
  {
    public int ProjectCharacterFieldDropdownValueId { get; set; }
    public int ProjectCharacterFieldId { get; set; }

    public virtual ProjectCharacterField ProjectCharacterField { get; set; }

    public int ProjectId { get; set; }

    public int Id => ProjectCharacterFieldDropdownValueId;

    public virtual Project Project { get; set; }

    bool IDeletableSubEntity.CanBePermanentlyDeleted => !WasEverUsed; 

    public bool IsActive { get; set; }

    public bool WasEverUsed { get; set; }

    public string Label { get; set; }

    public MarkdownString Description { get; set; }

    public virtual CharacterGroup CharacterGroup { get; set; }
    public int CharacterGroupId  { get; set; }
  }
}