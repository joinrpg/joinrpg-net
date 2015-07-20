namespace JoinRpg.DataModel
{
  public class ProjectCharacterField
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

    public bool IsActive { get; set; }

    public bool WasEverUsed { get; set; }
  }

  public enum CharacterFieldType
  {
    String,
    Text,
    Dropdown
  }
}