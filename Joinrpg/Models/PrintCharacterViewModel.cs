using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class PrintCharacterViewModel
  {
    public string ProjectName { get; set; }
    public string CharacterName { get; set; }
    public User Player { get; set; }
    public int FeeDue { get; set; }
    public bool RegistrationOnHold => FeeDue > 0;
  }
}