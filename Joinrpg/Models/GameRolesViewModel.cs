namespace JoinRpg.Web.Models
{
  public class GameRolesViewModel
  {
    public int ProjectId { get; set; }

    public string ProjectName{ get; set; }

    public bool ShowEditControls { get; set; }
    public CharacterGroupListViewModel Data { get; set; }
  }
}
