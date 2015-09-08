namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (used by LINQ)
  public class ProjectCharacterField : IProjectSubEntity, IDeletableSubEntity
  {
    public int ProjectCharacterFieldId
    { get; set; }

    public string FieldName { get; set; }

    public CharacterFieldType FieldType { get; set; }

    public bool IsPublic { get; set; }

    public bool CanPlayerView { get; set; }

    public bool CanPlayerEdit { get; set; }

    public string FieldHint { get; set; }

    public int Order { get; set; }

    public virtual Project Project { get; set; }

    public int ProjectId { get; set; }
    int IProjectSubEntity.Id => ProjectCharacterFieldId;

    public bool IsActive { get; set; }

    public bool WasEverUsed { get; set; }

    bool IDeletableSubEntity.CanBePermanentlyDeleted => !WasEverUsed;
  }

  public enum CharacterFieldType
  {
    String,
    Text,
    Dropdown
  }
}